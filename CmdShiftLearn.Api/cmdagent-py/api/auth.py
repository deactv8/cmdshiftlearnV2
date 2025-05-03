"""
Authentication module for CmdShiftLearn API key authentication.
Handles login, API key management, and authentication headers.
"""

import os
import json
import logging
from typing import Optional, Tuple
from rich.console import Console
from rich.prompt import Prompt

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.auth')
console = Console()

# Constants
API_KEY_FILE = os.path.expanduser("~/.cmdshiftlearn/apikey.json")


def ensure_storage_dir():
    """Ensure the storage directory exists"""
    os.makedirs(os.path.dirname(API_KEY_FILE), exist_ok=True)


def save_api_key(api_key: str) -> bool:
    """
    Save API key to local storage.
    
    Args:
        api_key: The API key to save
        
    Returns:
        bool: True if successful, False otherwise
    """
    try:
        ensure_storage_dir()
        with open(API_KEY_FILE, 'w') as f:
            json.dump({"api_key": api_key}, f)
        
        logger.debug(f"Saved API key (first 3 chars): {api_key[:3]}...")
        return True
    except Exception as e:
        logger.error(f"Error saving API key: {e}")
        return False


def load_api_key() -> Optional[str]:
    """
    Load API key from local storage.
    
    Returns:
        Optional[str]: The API key if found, None otherwise
    """
    try:
        if os.path.exists(API_KEY_FILE):
            with open(API_KEY_FILE, 'r') as f:
                data = json.load(f)
                api_key = data.get("api_key")
                if api_key:
                    logger.debug(f"Loaded API key (first 3 chars): {api_key[:3]}...")
                return api_key
    except Exception as e:
        logger.error(f"Error loading API key: {e}")
    
    return None


def clear_api_key() -> bool:
    """
    Clear the saved API key.
    
    Returns:
        bool: True if successful, False otherwise
    """
    try:
        if os.path.exists(API_KEY_FILE):
            os.remove(API_KEY_FILE)
            logger.debug("API key cleared")
            return True
    except Exception as e:
        logger.error(f"Error clearing API key: {e}")
    
    return False


def login() -> Tuple[bool, Optional[str], Optional[str]]:
    """
    Handle user login via API key.
    
    Returns:
        Tuple[bool, Optional[str], Optional[str]]: 
            (success, api_key, error_message)
    """
    # Check for existing API key
    api_key = load_api_key()
    
    if api_key:
        logger.info("Using existing API key")
        return True, api_key, None
    
    # No saved API key, prompt user for a new one
    console.print("""
╔════════════════════════════════════════════════════════════════╗
║                   Welcome to CmdShiftLearn!                    ║
║       The PowerShell learning platform that makes learning     ║
║                   fun and incredibly easy.                     ║
╚════════════════════════════════════════════════════════════════╝
    """)
    
    console.print("\nYou'll need an API key to continue.")
    console.print("For testing, use one of these keys:")
    console.print("  [cyan]devkey123[/cyan] - Developer test key")
    console.print("  [cyan]testkey456[/cyan] - Testing key\n")
    
    api_key = Prompt.ask("[bold cyan]Enter your API key[/bold cyan]")
    
    if not api_key:
        logger.error("No API key provided")
        return False, None, "API key is required to use CmdShiftLearn."
    
    # Save the API key for future use
    save_api_key(api_key)
    logger.info("New API key saved")
    
    return True, api_key, None


def logout() -> bool:
    """
    Log the user out by clearing saved API key.
    
    Returns:
        bool: True if successful, False otherwise
    """
    return clear_api_key()


def get_auth_header(api_key: Optional[str] = None) -> dict:
    """
    Get authorization headers with API key.
    Loads API key from file if not provided.
    
    Args:
        api_key: Optional API key to use
        
    Returns:
        dict: Headers dictionary with Authorization header
    """
    if not api_key:
        # Try to load the API key from file
        api_key = load_api_key()
    
    if not api_key:
        logger.warning("No API key available for authorization header")
        return {}  # Return empty headers if no API key is available
    
    # Create headers with API key authentication
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json",
        "Authorization": f"ApiKey {api_key}"
    }
    
    return headers
