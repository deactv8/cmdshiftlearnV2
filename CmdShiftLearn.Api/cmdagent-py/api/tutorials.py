"""
API client for interacting with the CmdShiftLearn tutorials API.
"""

import json
import logging
import httpx
from typing import List, Dict, Any, Optional

from utils.config import API_BASE_URL, USE_MOCK_DATA
from api.mock_data import TUTORIALS as MOCK_TUTORIALS
from api.auth import get_auth_header

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.tutorials')


class TutorialClient:
    """Client for interacting with the tutorials API."""
    
    def __init__(self):
        self.base_url = f"{API_BASE_URL}/tutorials"
    
    def get_tutorials(self) -> List[Dict[str, Any]]:
        """
        Fetch all available tutorials from the API.
        
        Returns:
            List[Dict[str, Any]]: A list of tutorial objects
        """
        logger.info(f"Fetching tutorials from {self.base_url}")
        
        try:
            # Get authentication headers
            headers = get_auth_header()
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.get(self.base_url, headers=headers)
            
            logger.info(f"API response status code: {response.status_code}")
            response.raise_for_status()  # Raise exception for 4XX/5XX responses
            
            tutorials = response.json()
            logger.info(f"Successfully fetched {len(tutorials)} tutorials")
            return tutorials
            
        except httpx.TimeoutException:
            logger.error("Request timed out while fetching tutorials")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error occurred: {e.response.status_code} - {e.response.reason_phrase}")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
            
        except httpx.RequestError as e:
            logger.error(f"Error fetching tutorials: {e}")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
            
        except json.JSONDecodeError:
            logger.error("Invalid JSON response received from API")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
            
        except Exception as e:
            logger.error(f"Unexpected error: {str(e)}")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
    
    def get_tutorial_by_id(self, tutorial_id: str) -> Optional[Dict[str, Any]]:
        """
        Fetch a specific tutorial by its ID.
        
        Args:
            tutorial_id: The ID of the tutorial to fetch
            
        Returns:
            Dict[str, Any] or None: The tutorial object if found, None otherwise
        """
        url = f"{self.base_url}/{tutorial_id}"
        logger.info(f"Fetching tutorial {tutorial_id} from {url}")
        
        try:
            # Get authentication headers
            headers = get_auth_header()
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.get(url, headers=headers)
            
            logger.info(f"API response status code: {response.status_code}")
            response.raise_for_status()
            
            tutorial = response.json()
            logger.info(f"Successfully fetched tutorial: {tutorial.get('title', 'Unknown')}")
            return tutorial
            
        except httpx.TimeoutException:
            logger.error(f"Request timed out while fetching tutorial {tutorial_id}")
            
        except httpx.HTTPStatusError as e:
            if e.response.status_code == 404:
                logger.warning(f"Tutorial with ID '{tutorial_id}' not found")
            elif e.response.status_code == 401:
                logger.error("Authentication required. Please log in first.")
            else:
                logger.error(f"HTTP Error: {e.response.status_code} - {e.response.reason_phrase}")
                
        except httpx.RequestError as e:
            logger.error(f"Error fetching tutorial {tutorial_id}: {e}")
            
        except json.JSONDecodeError:
            logger.error(f"Invalid JSON response received for tutorial {tutorial_id}")
            
        except Exception as e:
            logger.error(f"Unexpected error fetching tutorial {tutorial_id}: {str(e)}")
        
        # Return mock tutorial if it exists in our fallback data and USE_MOCK_DATA is True
        if USE_MOCK_DATA:
            logger.info(f"Attempting to use mock data for tutorial {tutorial_id}")
            for tutorial in MOCK_TUTORIALS:
                if tutorial["id"] == tutorial_id:
                    logger.info(f"Returning mock data for tutorial {tutorial_id}")
                    return tutorial
                    
            logger.warning(f"No mock data available for tutorial {tutorial_id}")
        
        return None