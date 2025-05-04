"""
Configuration settings for CmdShiftLearn.
"""

import os
import json
from pathlib import Path

# Base directory for the application
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Data directory for storing content and user data
DATA_DIR = os.path.join(BASE_DIR, "data")

# API settings
API_BASE_URL = "https://cmdshiftlearn-api.azurewebsites.net/api"
API_VERSION = "v1"

# Default PowerShell settings
DEFAULT_POWERSHELL_TIMEOUT = 10  # seconds

# Application settings
APP_NAME = "CmdShiftLearn"
APP_VERSION = "0.1.0"

# Default user settings
DEFAULT_USER_SETTINGS = {
    "theme": "dark",
    "notifications": True,
    "sound_effects": True
}

# Create directories if they don't exist
def ensure_directories():
    """Create necessary directories if they don't exist."""
    directories = [
        DATA_DIR,
        os.path.join(DATA_DIR, "content"),
        os.path.join(DATA_DIR, "content", "tutorials"),
        os.path.join(DATA_DIR, "content", "challenges"),
        os.path.join(DATA_DIR, "content", "certifications"),
        os.path.join(DATA_DIR, "content", "skill_trees"),
        os.path.join(DATA_DIR, "users"),
        os.path.join(DATA_DIR, "history"),
    ]
    
    for directory in directories:
        os.makedirs(directory, exist_ok=True)

# Create a default configuration file
def create_default_config():
    """Create a default configuration file."""
    config = {
        "api": {
            "base_url": API_BASE_URL,
            "version": API_VERSION
        },
        "powershell": {
            "timeout": DEFAULT_POWERSHELL_TIMEOUT
        },
        "app": {
            "name": APP_NAME,
            "version": APP_VERSION
        },
        "user": {
            "default_settings": DEFAULT_USER_SETTINGS
        }
    }
    
    config_path = os.path.join(DATA_DIR, "config.json")
    
    with open(config_path, 'w') as f:
        json.dump(config, f, indent=2)

# Load configuration from file
def load_config():
    """
    Load configuration from file.
    
    Returns:
        dict: Configuration data
    """
    config_path = os.path.join(DATA_DIR, "config.json")
    
    if not os.path.exists(config_path):
        ensure_directories()
        create_default_config()
    
    with open(config_path, 'r') as f:
        return json.load(f)

# Initialize directories
ensure_directories()
