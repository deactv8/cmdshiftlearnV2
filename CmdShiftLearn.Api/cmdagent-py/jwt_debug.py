#!/usr/bin/env python3
"""
JWT Token Debugging Script

This script loads and decodes the JWT token stored in token.json,
extracts and displays the audience claim, and checks if it matches
what the API expects.
"""

import os
import json
import base64
import logging
from typing import Dict, Any, Optional

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('jwt_debug')

TOKEN_FILE = "token.json"

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

def load_and_decode_token() -> Optional[Dict[str, Any]]:
    """
    Load the JWT token from file and decode it.
    
    Returns:
        Optional[Dict[str, Any]]: Decoded token data if successful
    """
    try:
        if not os.path.exists(TOKEN_FILE):
            logger.error(f"Token file {TOKEN_FILE} does not exist")
            return None
            
        with open(TOKEN_FILE, 'r') as f:
            token_data = json.load(f)
            token = token_data.get('access_token')
            
            if not token:
                logger.error("No access_token found in token data")
                return None
                
            logger.info(f"Token found (first 10 chars): {token[:10]}...")
            
            # Split the token
            parts = token.split('.')
            if len(parts) < 2:
                logger.error("Invalid JWT token format (not enough parts)")
                return None
                
            # Decode header and payload
            header = decode_jwt_part(parts[0])
            payload = decode_jwt_part(parts[1])
            
            # Create decoded token object
            decoded_token = {
                "header": header,
                "payload": payload,
                "raw_token": token
            }
            
            return decoded_token
            
    except Exception as e:
        logger.error(f"Error loading or decoding token: {e}")
        return None

def analyze_token(decoded_token: Dict[str, Any]) -> None:
    """
    Analyze the token and its claims.
    
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
    
    # Extract payload info
    payload = decoded_token.get("payload", {})
    
    # Extract basic claims
    logger.info(f"Issuer (iss): {payload.get('iss', 'Not specified')}")
    logger.info(f"Subject (sub): {payload.get('sub', 'Not specified')}")
    logger.info(f"Audience (aud): {payload.get('aud', 'Not specified')}")
    
    # Check expiration
    exp = payload.get('exp')
    if exp:
        import time
        now = int(time.time())
        if exp < now:
            logger.warning(f"Token is expired! Expired {now - exp} seconds ago")
        else:
            logger.info(f"Token is valid for {exp - now} more seconds")
    
    # Check audience specifically
    aud = payload.get('aud')
    if aud:
        if aud == 'authenticated':
            logger.info("Audience claim is 'authenticated' - this matches what the API expects")
        else:
            logger.warning(f"Audience claim is '{aud}' - the API expects 'authenticated'")
            print(f"ISSUE DETECTED: Token audience is '{aud}' but API expects 'authenticated'")
    else:
        logger.warning("No audience claim found in token")
        print("ISSUE DETECTED: Token has no audience claim, but API expects 'authenticated'")

def main():
    """Main entry point for the script."""
    print("=== JWT Token Debug Tool ===")
    
    # Load and decode token
    decoded_token = load_and_decode_token()
    
    if decoded_token:
        print("\n=== Token Analysis ===")
        analyze_token(decoded_token)
        
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
