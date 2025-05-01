"""
API client for interacting with the CmdShiftLearn challenges API.
"""

import json
import logging
import httpx
from typing import List, Dict, Any, Optional

from utils.config import API_BASE_URL, USE_MOCK_DATA
from api.auth import get_auth_header, SUPABASE_API_KEY

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.challenges')

# Mock challenges data for fallback
MOCK_CHALLENGES = [
    {
        "id": "daily-powershell",
        "title": "Daily PowerShell Challenge",
        "description": "Complete a short PowerShell challenge to earn XP",
        "difficulty": "beginner",
        "xp_reward": 50,
        "is_daily": True
    },
    {
        "id": "git-advanced",
        "title": "Advanced Git Operations",
        "description": "Show your Git mastery by completing complex operations",
        "difficulty": "advanced",
        "xp_reward": 150,
        "is_daily": False
    },
    {
        "id": "python-algorithms",
        "title": "Python Algorithm Challenge",
        "description": "Implement common algorithms in Python",
        "difficulty": "intermediate",
        "xp_reward": 100,
        "is_daily": False
    }
]


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
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_CHALLENGES
            return []
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error occurred: {e.response.status_code} - {e.response.reason_phrase}")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_CHALLENGES
            return []
            
        except httpx.RequestError as e:
            logger.error(f"Error fetching challenges: {e}")
            if USE