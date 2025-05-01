"""
Authentication module for Supabase integration.
Handles login, token management, and authentication headers.
"""

import os
import json
import logging
import httpx
from typing import Dict, Any, Optional, Tuple

# Configure logging with more details
logging.basicConfig(
    level=logging.DEBUG,  # Change to DEBUG for more detailed logs
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.auth')

# Supabase configuration
SUPABASE_PROJECT_URL = "https://fqceiphubiqnorytayiu.supabase.co"
SUPABASE_AUTH_URL = f"{SUPABASE_PROJECT_URL}/auth/v1"
SUPABASE_SIGNIN_URL = f"{SUPABASE_AUTH_URL}/token?grant_type=password"
SUPABASE_SIGNUP_URL = f"{SUPABASE_AUTH_URL}/signup"
# The correct anon key from Supabase project
SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZxY2VpcGh1Ymlxbm9yeXRheWl1Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDQ3NzcyMjAsImV4cCI6MjA2MDM1MzIyMH0.iXTLdfgAZZzcoeO7P9k4Z81yiqhrm-GztgxxzUYg-14"
TOKEN_FILE = "token.json"


def load_token() -> Optional[str]:
    """
    Load JWT token from file.
    
    Returns:
        Optional[str]: The access token if found, None otherwise
    """
    try:
        if os.path.exists(TOKEN_FILE):
            with open(TOKEN_FILE, 'r') as f:
                data = json.load(f)
                return data.get('access_token')
    except Exception as e:
        logger.error(f"Error loading token: {e}")
    
    return None


def save_token(token_data: Dict[str, Any]) -> bool:
    """
    Save token data to file.
    
    Args:
        token_data: The token data to save
        
    Returns:
        bool: True if successful, False otherwise
    """
    try:
        # Extract the necessary information from token data
        # Make sure we save the full response for better debugging
        with open(TOKEN_FILE, 'w') as f:
            json.dump(token_data, f)
            
        # Also log the token type and some metadata to help debug
        if 'access_token' in token_data:
            token = token_data['access_token']
            logger.debug(f"Saved token (first 10 chars): {token[:10]}...")
            logger.debug(f"Token data keys: {token_data.keys()}")
            
        return True
    except Exception as e:
        logger.error(f"Error saving token: {e}")
        return False


def login(email: str, password: str) -> Tuple[bool, Optional[str], Optional[str], Optional[Dict]]:
    """
    Log in using Supabase authentication.
    
    Args:
        email: User's email
        password: User's password
        
    Returns:
        Tuple[bool, Optional[str], Optional[str], Optional[Dict]]: 
            (success, token, error_message, error_details)
    """
    headers = {
        "Content-Type": "application/json",
        "apikey": SUPABASE_API_KEY
    }
    
    # Fix payload format according to Supabase documentation
    payload = {
        "email": email,
        "password": password,
        "grant_type": "password"  # Include grant_type in payload as well
    }
    
    try:
        logger.info(f"Attempting to login with email: {email}")
        logger.debug(f"Authentication URL: {SUPABASE_PROJECT_URL}/auth/v1/token?grant_type=password")
        logger.debug(f"Request headers: {headers}")
        logger.debug(f"Request payload: {payload}")
        
        with httpx.Client(timeout=10.0) as client:
            # Use the signin URL
            response = client.post(
                SUPABASE_SIGNIN_URL, 
                headers=headers,
                json=payload
            )
        
        logger.info(f"Login response status code: {response.status_code}")
        # Log the entire response for debugging
        error_details = None
        try:
            response_data = response.json()
            logger.debug(f"Response data: {response_data}")
            
            # Extract error details if available
            if response.status_code != 200:
                error_details = response_data
        except:
            logger.debug(f"Response text: {response.text}")
        
        if response.status_code == 200:
            data = response.json()
            access_token = data.get('access_token')
            
            if access_token:
                # Save token data to file
                save_token(data)
                logger.info("Login successful, token saved")
                return True, access_token, None, None
            else:
                logger.error("No access token in response")
                return False, None, "Authentication failed: No access token received", None
        else:
            error_msg = f"Authentication failed: {response.status_code}"
            
            # Try to extract error message from response
            try:
                error_data = response.json()
                if 'error' in error_data:
                    error_msg = f"Authentication failed: {error_data['error']}"
                elif 'error_code' in error_data:
                    error_msg = f"Authentication failed: {error_data['error_code']}"
                elif 'message' in error_data:
                    error_msg = f"Authentication failed: {error_data['message']}"
                elif 'msg' in error_data:
                    error_msg = f"Authentication failed: {error_data['msg']}"
            except:
                pass
                
            logger.error(error_msg)
            return False, None, error_msg, error_details
            
    except httpx.TimeoutException:
        error_msg = "Authentication failed: Request timed out"
        logger.error(error_msg)
        return False, None, error_msg, None
        
    except Exception as e:
        error_msg = f"Authentication failed: {str(e)}"
        logger.error(error_msg)
        return False, None, error_msg, None


def signup(email: str, password: str) -> Tuple[bool, Optional[str], Optional[str]]:
    """
    Sign up a new user with Supabase authentication.
    
    Args:
        email: User's email
        password: User's password
        
    Returns:
        Tuple[bool, Optional[str], Optional[str]]: 
            (success, token, error_message)
    """
    headers = {
        "Content-Type": "application/json",
        "apikey": SUPABASE_API_KEY
    }
    
    payload = {
        "email": email,
        "password": password
    }
    
    try:
        logger.info(f"Attempting to sign up with email: {email}")
        logger.debug(f"Signup URL: {SUPABASE_SIGNUP_URL}")
        logger.debug(f"Request headers: {headers}")
        logger.debug(f"Request payload: {payload}")
        
        with httpx.Client(timeout=10.0) as client:
            response = client.post(
                SUPABASE_SIGNUP_URL, 
                headers=headers,
                json=payload
            )
        
        logger.info(f"Signup response status code: {response.status_code}")
        
        # Log the response for debugging
        try:
            response_data = response.json()
            logger.debug(f"Response data: {response_data}")
        except:
            logger.debug(f"Response text: {response.text}")
        
        if response.status_code == 200:
            data = response.json()
            access_token = data.get('access_token')
            
            # For Supabase, signup doesn't return an access token immediately if email confirmation is enabled
            # Instead, it returns user data with confirmation_sent_at field
            if 'confirmation_sent_at' in data:
                logger.info("Signup successful, confirmation email sent")
                return True, None, "Account created successfully! Please check your email to confirm your account, then log in."
            elif access_token:
                # If email confirmation is not required, we might get an access token
                save_token(data)
                logger.info("Signup successful, token saved")
                return True, access_token, None
            else:
                logger.error("No access token in response")
                return False, None, "Signup successful but no access token received. Try logging in."
        else:
            error_msg = f"Signup failed: {response.status_code}"
            
            # Try to extract error message from response
            try:
                error_data = response.json()
                if 'error' in error_data:
                    error_msg = f"Signup failed: {error_data['error']}"
                elif 'message' in error_data:
                    error_msg = f"Signup failed: {error_data['message']}"
                elif 'msg' in error_data:
                    error_msg = f"Signup failed: {error_data['msg']}"
            except:
                pass
                
            logger.error(error_msg)
            return False, None, error_msg
            
    except httpx.TimeoutException:
        error_msg = "Signup failed: Request timed out"
        logger.error(error_msg)
        return False, None, error_msg
        
    except Exception as e:
        error_msg = f"Signup failed: {str(e)}"
        logger.error(error_msg)
        return False, None, error_msg


def get_auth_header(token: Optional[str] = None) -> Dict[str, str]:
    """
    Get authorization headers with token.
    Loads token from file if not provided.
    Only sends the Authorization header to the API.
    
    Args:
        token: Optional token to use
        
    Returns:
        Dict[str, str]: Headers dictionary with Authorization header
    """
    import base64
    
    if not token:
        # Try to load the token from file
        token = load_token()
        
        # Debug the token if we found one
        if token:
            try:
                # Function to decode JWT token parts
                def decode_jwt_part(part):
                    # Add padding if needed
                    padded = part + '=' * (4 - len(part) % 4)
                    try:
                        decoded = base64.urlsafe_b64decode(padded).decode('utf-8')
                        return json.loads(decoded)
                    except Exception as e:
                        logger.error(f"Error decoding JWT part: {e}")
                        return {}
                
                # Split the token into parts
                parts = token.split('.')
                if len(parts) >= 2:
                    # Decode the header and payload
                    header = decode_jwt_part(parts[0])
                    payload = decode_jwt_part(parts[1])
                    
                    # Log the important parts
                    logger.debug(f"JWT header: {header}")
                    logger.debug(f"JWT payload: {payload}")
                    
                    # Check for the audience claim
                    if 'aud' in payload:
                        logger.debug(f"JWT audience: {payload['aud']}")
                    
                    # Check for issuer
                    if 'iss' in payload:
                        logger.debug(f"JWT issuer: {payload['iss']}")
                        
                    # Log signature verification info
                    if 'kid' in header:
                        logger.debug(f"JWT key ID: {header['kid']}")
                        logger.debug("NOTE: API needs this key ID to verify the token")
            except Exception as e:
                logger.error(f"Error parsing JWT token: {e}")
    
    if not token:
        logger.warning("No token available for authorization header")
        return {}  # Return empty headers if no token is available
    
    # Add debug logging to trace the token
    logger.debug(f"Using auth token (first 10 chars): {token[:10]}...")
    
    # Create headers with only Authorization as required by the API
    # The token is correctly formatted with Bearer prefix
    headers = {
        "Authorization": f"Bearer {token}"
    }
    
    return headers
