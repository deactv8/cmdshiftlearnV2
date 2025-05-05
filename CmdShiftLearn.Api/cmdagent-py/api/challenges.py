"""
API client for interacting with the CmdShiftLearn challenges API.
"""

import json
import logging
import httpx
from typing import List, Dict, Any, Optional

# Import only what's needed
from utils import API_BASE_URL
from api.auth import get_auth_header

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.challenges')


class ChallengeClient:
    """Client for interacting with the challenges API."""
    
    def __init__(self):
        self.base_url = f"{API_BASE_URL}/challenges"
    
    def get_challenges(self) -> List[Dict[str, Any]]:
        """
        Fetch all available challenges from the API.
        
        Returns:
            List[Dict[str, Any]]: A list of challenge objects
        """
        logger.info(f"Fetching challenges from {self.base_url}")
        
        try:
            # Get authentication headers (includes apikey)
            headers = get_auth_header()
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.get(self.base_url, headers=headers)
            
            logger.info(f"API response status code: {response.status_code}")
            response.raise_for_status()  # Raise exception for 4XX/5XX responses
            
            challenges = response.json()
            logger.info(f"Successfully fetched {len(challenges)} challenges")
            return challenges
            
        except httpx.TimeoutException:
            logger.error("Request timed out while fetching challenges")
            return []
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error occurred: {e.response.status_code} - {e.response.reason_phrase}")
            return []
            
        except httpx.RequestError as e:
            logger.error(f"Error fetching challenges: {e}")
            return []