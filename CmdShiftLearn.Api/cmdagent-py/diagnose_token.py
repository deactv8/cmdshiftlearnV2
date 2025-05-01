#!/usr/bin/env python3
"""
JWT Token Diagnostics Tool

This script performs a comprehensive analysis of the JWT token and attempts to
diagnose why the API is rejecting it. It specifically checks for signature and
audience validation issues.
"""

import os
import json
import base64
import logging
import httpx
import time
from typing import Dict, Any, Optional, Tuple
from datetime import datetime, timezone

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('jwt_diagnose')

TOKEN_FILE = "token.json"
API_BASE_URL = "https://cmdshiftlearnv2.onrender.com/api"


def decode_jwt_part(part: str) -> Dict[str, Any]:
    """
    Decode a single part of a JWT token.
    
    Args:
        part: Base64 encoded part of JWT token
    
    Returns:
        Dict[str, Any]: Decoded JSON content
    """
    # Add padding if needed
    padded = part + '=' * (4 - len(part) % 4)
    try:
        decoded = base64.urlsafe_b64decode(padded).decode('utf-8')
        return json.loads(decoded)
    except Exception as e:
        logger.error(f"Error decoding JWT part: {e}")
        return {}


def load_and_decode_token() -> Tuple[Optional[str], Optional[Dict[str, Any]]]:
    """
    Load the JWT token from file and decode it.
    
    Returns:
        Tuple[Optional[str], Optional[Dict[str, Any]]]: (raw_token, decoded_token)
    """
    try:
        if not os.path.exists(TOKEN_FILE):
            logger.error(f"Token file {TOKEN_FILE} does not exist")
            return None, None
            
        with open(TOKEN_FILE, 'r') as f:
            token_data = json.load(f)
            token = token_data.get('access_token')
            
            if not token:
                logger.error("No access_token found in token data")
                return None, None
                
            logger.info(f"Token found (first 10 chars): {token[:10]}...")
            
            # Split the token
            parts = token.split('.')
            if len(parts) < 2:
                logger.error("Invalid JWT token format (not enough parts)")
                return token, None
                
            # Decode header and payload
            header = decode_jwt_part(parts[0])
            payload = decode_jwt_part(parts[1])
            
            # Create decoded token object
            decoded_token = {
                "header": header,
                "payload": payload,
                "raw_token": token
            }
            
            return token, decoded_token
            
    except Exception as e:
        logger.error(f"Error loading or decoding token: {e}")
        return None, None


def analyze_token(decoded_token: Dict[str, Any]) -> None:
    """
    Perform a comprehensive analysis of the token.
    
    Args:
        decoded_token: Decoded token data
    """
    if not decoded_token:
        logger.error("No token to analyze")
        return
        
    # Extract header info
    header = decoded_token.get("header", {})
    logger.info(f"Token type: {header.get('typ', 'Unknown')}")
    logger.info(f"Token algorithm: {header.get('alg', 'Unknown')}")
    
    # Check for kid (key ID)
    kid = header.get('kid')
    if kid:
        logger.info(f"Key ID (kid): {kid}")
    else:
        logger.warning("No Key ID (kid) in header - API may not be able to find the signature key")
    
    # Extract payload info
    payload = decoded_token.get("payload", {})
    
    # Extract and check issuer
    issuer = payload.get('iss')
    if issuer:
        logger.info(f"Issuer (iss): {issuer}")
        if "supabase" in issuer.lower():
            logger.warning("Token is issued by Supabase, but API may be expecting a different issuer")
    else:
        logger.warning("No issuer claim found in token")
    
    # Extract and check subject
    subject = payload.get('sub')
    if subject:
        logger.info(f"Subject (sub): {subject}")
    else:
        logger.warning("No subject claim found in token")
    
    # Extract and check audience
    audience = payload.get('aud')
    if audience:
        logger.info(f"Audience (aud): {audience}")
        if audience != "authenticated":
            logger.warning(f"Audience claim is '{audience}' - the API expects 'authenticated'")
    else:
        logger.warning("No audience claim found in token")
    
    # Check expiration
    exp = payload.get('exp')
    if exp:
        now = int(time.time())
        if exp < now:
            logger.warning(f"Token is expired! Expired {now - exp} seconds ago")
            expiry_time = datetime.fromtimestamp(exp).strftime('%Y-%m-%d %H:%M:%S')
            logger.info(f"Token expired at: {expiry_time}")
        else:
            logger.info(f"Token is valid for {exp - now} more seconds")
            expiry_time = datetime.fromtimestamp(exp).strftime('%Y-%m-%d %H:%M:%S')
            logger.info(f"Token expires at: {expiry_time}")
    else:
        logger.warning("No expiration claim found in token")
    
    # Check issued at
    iat = payload.get('iat')
    if iat:
        issuance_time = datetime.fromtimestamp(iat).strftime('%Y-%m-%d %H:%M:%S')
        logger.info(f"Token issued at: {issuance_time}")
    else:
        logger.warning("No issued-at claim found in token")
    
    # Check for additional claims that might be relevant
    if 'role' in payload:
        logger.info(f"Role claim: {payload['role']}")
    
    # Print full payload for reference
    logger.debug(f"Full payload: {json.dumps(payload, indent=2)}")


def test_api_endpoints(token: str) -> None:
    """
    Test various API endpoints to diagnose token issues.
    
    Args:
        token: Raw JWT token
    """
    logger.info("Testing API endpoints with the token...")
    
    # Define endpoints to test
    endpoints = [
        "/tutorials",
        "/tutorials/powershell-basics-1"
    ]
    
    # Try with different header formats
    header_formats = [
        {"Authorization": f"Bearer {token}"},
        {"Authorization": token},
    ]
    
    for endpoint in endpoints:
        url = f"{API_BASE_URL}{endpoint}"
        logger.info(f"Testing endpoint: {url}")
        
        for headers in header_formats:
            header_desc = list(headers.keys())[0] + " = " + headers[list(headers.keys())[0]][:15] + "..."
            logger.info(f"Using header format: {header_desc}")
            
            try:
                with httpx.Client(timeout=10.0) as client:
                    response = client.get(url, headers=headers)
                
                logger.info(f"Response status code: {response.status_code}")
                
                # Check for specific error messages
                if response.status_code == 401:
                    www_authenticate = response.headers.get('www-authenticate', '')
                    logger.error(f"Authentication failed. WWW-Authenticate: {www_authenticate}")
                    
                    # Extract detailed error information
                    if "error_description=" in www_authenticate:
                        try:
                            error_description = www_authenticate.split('error_description=')[1].split('"')[1]
                            logger.error(f"Error description: {error_description}")
                            
                            # Analyze specific error types
                            if "signature key was not found" in error_description:
                                logger.error("DIAGNOSIS: The API cannot verify the token signature.")
                                logger.error("This could be because:")
                                logger.error("1. The API is expecting a token from a different issuer")
                                logger.error("2. The key ID (kid) in the token header is not recognized by the API")
                                logger.error("3. The token was signed with a key that the API doesn't have")
                            elif "audience" in error_description:
                                logger.error("DIAGNOSIS: The audience claim in the token is invalid.")
                                logger.error("The API expects the audience to be 'authenticated'")
                            elif "expired" in error_description:
                                logger.error("DIAGNOSIS: The token has expired.")
                            else:
                                logger.error(f"DIAGNOSIS: Unknown error: {error_description}")
                        except:
                            logger.error("Could not extract error description")
                elif response.status_code == 200:
                    logger.info("SUCCESS: API accepted the token!")
                    return
            except Exception as e:
                logger.error(f"Error testing endpoint: {e}")
    
    logger.error("All API endpoint tests failed. The token is not being accepted by any endpoint.")


def main():
    """Main entry point for the script."""
    print("=== JWT Token Diagnostic Tool ===")
    
    # Load and decode token
    token, decoded_token = load_and_decode_token()
    
    if token and decoded_token:
        print("\n=== Token Analysis ===")
        analyze_token(decoded_token)
        
        print("\n=== API Endpoint Tests ===")
        test_api_endpoints(token)
        
        # Print full decoded token for reference
        print("\n=== Token Details ===")
        header = json.dumps(decoded_token.get("header", {}), indent=2)
        payload = json.dumps(decoded_token.get("payload", {}), indent=2)
        
        print(f"Header:\n{header}")
        print(f"\nPayload:\n{payload}")
    else:
        print("Failed to load or decode token")


if __name__ == "__main__":
    main()
