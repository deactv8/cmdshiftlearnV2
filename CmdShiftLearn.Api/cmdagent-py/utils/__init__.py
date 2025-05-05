"""
Utility modules for the cmdagent-py CLI tool.
"""

# Explicitly import and expose config variables
from .config import (
    BASE_DIR, DATA_DIR, API_BASE_URL, API_VERSION, 
    DEFAULT_POWERSHELL_TIMEOUT, APP_NAME, APP_VERSION, DEFAULT_USER_SETTINGS,
    ensure_directories, create_default_config, load_config
)

__all__ = [
    'BASE_DIR', 'DATA_DIR', 'API_BASE_URL', 'API_VERSION',
    'DEFAULT_POWERSHELL_TIMEOUT', 'APP_NAME', 'APP_VERSION', 'DEFAULT_USER_SETTINGS',
    'ensure_directories', 'create_default_config', 'load_config'
]