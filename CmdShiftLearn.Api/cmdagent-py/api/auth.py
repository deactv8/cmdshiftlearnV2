"""
Authentication module for Supabase integration.
Handles login, token management, and authentication headers.
"""

import os
import json
import logging
import httpx
from typing import Dict, Any, Optional, Tuple

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.auth')

# Supabase configuration
SUPABASE_PROJECT_URL = "https://fqceiphubiqnorytayiu.supabase.co"
SUPABASE_AUTH_URL = f"{SUPABASE_PROJECT_URL}/auth/v1/token"
# The correct anon key from Supabase project settings
SUPABASE_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZxY2VpcGh1Ymlxbm9yeXRheWl1Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODI1MDg4MDAsImV4cCI6MTk5ODA4NDgwMH0.eyJpcCJ3OiCII6IjY1LjEwOC4yMDAuOTAiLCJpYXQiOjE2ODI1MDkwMzF9"
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
        with open(TOKEN_FILE, 'w') as f:
            json.dump(token_data, f)
        return True
    except Exception as e:
        logger.error(f"Error saving token: {e}")
        return False


def login(email: str, password: str) -> Tuple[bool, Optional[str], Optional[str]]:
    """
    Log in using Supabase authentication.
    
    Args:
        email: User's email
        password: User's password
        
    Returns:
        Tuple[bool, Optional[str], Optional[str]]: 
            (success, token, error_message)
    """
    headers = {
        "Content-Type": "application/json",
        "apikey": SUPABASE_API_KEY,
        "Authorization": f"Bearer {SUPABASE_API_KEY}"
    }
    
    payload = {
        "email": email,
        "password": password
    }
    
    try:
        logger.info(f"Attempting to login with email: {email}")
        with httpx.Client(timeout=10.0) as client:
            response = client.post(
                f"{SUPABASE_AUTH_URL}?grant_type=password", 
                headers=headers,
                json=payload
            )
        
        logger.info(f"Login response status code: {response.status_code}")
        
        if response.status_code == 200:
            data = response.json()
            access_token = data.get('access_token')
            
            if access_token:
                # Save token data to file
                save_token(data)
                logger.info("Login successful, token saved")
                return True, access_token, None
            else:
                logger.error("No access token in response")
                return False, None, "Authentication failed: No access token received"
        else:
            error_msg = f"Authentication failed: {response.status_code}"
            
            # Try to extract error message from response
            try:
                error_data = response.json()
                if 'error' in error_data:
                    error_msg = f"Authentication failed: {error_data['error']}"
                elif 'message' in error_data:
                    error_msg = f"Authentication failed: {error_data['message']}"
            except:
                pass
                
            logger.error(error_msg)
            return False, None, error_msg
            
    except httpx.TimeoutException:
        error_msg = "Authentication failed: Request timed out"
        logger.error(error_msg)
        return False, None, error_msg
        
    except Exception as e:
        error_msg = f"Authentication failed: {str(e)}"
        logger.error(error_msg)
        return False, None, error_msg


def get_auth_header(token: Optional[str] = None) -> Dict[str, str]:
    """
    Get authorization headers with token.
    Loads token from file if not provided.
    For Supabase, both apikey and Authorization headers are needed.
    
    Args:
        token: Optional token to use
        
    Returns:
        Dict[str, str]: Headers dictionary with Authorization and apikey
    """
    if not token:
        token = load_token()
    
    if not token:
        logger.warning("No token available for authorization header")
        return {"apikey": SUPABASE_API_KEY}  # Return at least the apikey for anonymous access
    
    return {
        "apikey": SUPABASE_API_KEY,
        "Authorization": f"Bearer {token}"
    }
