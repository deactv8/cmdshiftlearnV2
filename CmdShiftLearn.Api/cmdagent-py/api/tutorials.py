"""
API client for interacting with the CmdShiftLearn tutorials API.
"""

import json
import logging
import httpx
from typing import List, Dict, Any, Optional

from utils.config import API_BASE_URL, USE_MOCK_DATA
from api.mock_data import TUTORIALS as MOCK_TUTORIALS
from api.auth import get_auth_header, SUPABASE_API_KEY

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
            # Get authentication headers (includes apikey)
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
            if e.response.status_code == 401:
                error_msg = "Authentication failed. Please check your login or contact support."
                logger.error(error_msg)
                print(error_msg)
            
            # Only fall back to mock data if USE_MOCK_DATA is explicitly set to True
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
            # Get authentication headers (includes apikey)
            headers = get_auth_header()
            
            # Log the headers being sent (excluding sensitive parts)
            logger.debug(f"Request headers: {headers.keys()}")
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.get(url, headers=headers)
            
            logger.info(f"API response status code: {response.status_code}")
            
            # Log the response headers for debugging
            logger.debug(f"Response headers: {dict(response.headers)}")
            
            # If we got a 401, log more detailed information and show clear error message
            if response.status_code == 401:
                logger.error(f"Authorization failed. Response details: {response.text}")
                error_msg = "Authentication failed. Please check your login or contact support."
                logger.error(error_msg)
                print(error_msg)
                
                # Only fall back to mock data if USE_MOCK_DATA is explicitly set to True
                if USE_MOCK_DATA:
                    logger.info("Falling back to mock data due to authentication failure")
                    for mock_tutorial in MOCK_TUTORIALS:
                        if mock_tutorial.get('id') == tutorial_id:
                            logger.info(f"Found mock data for {tutorial_id}")
                            return mock_tutorial
                    
                    logger.warning(f"No mock data found for tutorial ID: {tutorial_id}")
                    return None
                return None
                
            # Continue with the normal flow if no 401 error
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
                error_msg = "Authentication failed. Please check your login or contact support."
                logger.error(error_msg)
                print(error_msg)
                # Log additional information about authentication headers
                try:
                    www_authenticate = e.response.headers.get('www-authenticate', '')
                    logger.error(f"Authentication challenge: {www_authenticate}")
                except Exception as header_error:
                    logger.error(f"Error extracting headers: {str(header_error)}")
            else:
                logger.error(f"HTTP Error: {e.response.status_code} - {e.response.reason_phrase}")
                
            # Fall back to mock data only if USE_MOCK_DATA is explicitly set to True
            if USE_MOCK_DATA:
                logger.info(f"Attempting to use mock data for tutorial {tutorial_id}")
                for mock in MOCK_TUTORIALS:
                    if mock["id"] == tutorial_id:
                        logger.info(f"Found mock data for {tutorial_id}")
                        return mock
                
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