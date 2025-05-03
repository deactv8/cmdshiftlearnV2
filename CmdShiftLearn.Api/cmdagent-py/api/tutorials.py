"""
API client for interacting with the CmdShiftLearn tutorials API.
"""

import json
import logging
import httpx
from typing import List, Dict, Any, Optional
from rich.console import Console

from utils.config import API_BASE_URL, USE_MOCK_DATA
from api.mock_data import TUTORIALS as MOCK_TUTORIALS
from api.auth import get_auth_header

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('api.tutorials')
console = Console()


class TutorialClient:
    """Client for interacting with the tutorials API."""
    
    def __init__(self, api_key=None):
        self.base_url = f"{API_BASE_URL}/tutorials"
        self.api_key = api_key
    
    def get_tutorials(self) -> List[Dict[str, Any]]:
        """
        Fetch all available tutorials from the API.
        
        Returns:
            List[Dict[str, Any]]: A list of tutorial objects
        """
        logger.info(f"Fetching tutorials from {self.base_url}")
        
        try:
            # Get authentication headers with API key
            headers = get_auth_header(self.api_key)
            
            # Log the headers being sent (excluding sensitive parts)
            logger.debug(f"Request headers: {headers.keys()}")
            
            # If Authorization header is missing, log a warning
            if 'Authorization' not in headers:
                logger.warning("Authorization header is missing. Authentication will likely fail.")
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.get(self.base_url, headers=headers)
            
            logger.info(f"API response status code: {response.status_code}")
            
            # Handle authentication failure
            if response.status_code == 401:
                logger.error(f"Authentication failed. Response: {response.text}")
                console.print("[red]Authentication failed: Invalid API key[/red]")
                
                # Fall back to mock data if needed
                if USE_MOCK_DATA:
                    logger.info("Using mock data as fallback")
                    return MOCK_TUTORIALS
                return []
                
            response.raise_for_status()  # Raise exception for other 4XX/5XX responses
            
            tutorials = response.json()
            logger.info(f"Successfully fetched {len(tutorials)} tutorials")
            return tutorials
            
        except httpx.TimeoutException:
            logger.error("Request timed out while fetching tutorials")
            console.print("[red]Request timed out. Please try again later.[/red]")
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error occurred: {e.response.status_code} - {e.response.reason_phrase}")
            console.print(f"[red]Error: {e.response.reason_phrase}[/red]")
            
            # Only fall back to mock data if USE_MOCK_DATA is explicitly set to True
            if USE_MOCK_DATA:
                logger.info("Using mock data as fallback")
                return MOCK_TUTORIALS
            return []
            
        except Exception as e:
            logger.error(f"Unexpected error: {str(e)}")
            console.print(f"[red]Error connecting to CmdShiftLearn API: {str(e)}[/red]")
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
            # Get authentication headers with API key
            headers = get_auth_header(self.api_key)
            
            # Log the headers being sent (excluding sensitive parts)
            logger.debug(f"Request headers: {headers.keys()}")
            
            # If Authorization header is missing, log a warning
            if 'Authorization' not in headers:
                logger.warning("Authorization header is missing. Authentication will likely fail.")
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.get(url, headers=headers)
            
            logger.info(f"API response status code: {response.status_code}")
            
            # Handle authentication failure
            if response.status_code == 401:
                logger.error(f"Authentication failed. Response: {response.text}")
                console.print("[red]Authentication failed: Invalid API key[/red]")
                
                # Fall back to mock data if needed
                if USE_MOCK_DATA:
                    logger.info("Falling back to mock data due to authentication failure")
                    for mock_tutorial in MOCK_TUTORIALS:
                        if mock_tutorial.get('id') == tutorial_id:
                            logger.info(f"Found mock data for {tutorial_id}")
                            return mock_tutorial
                    
                    logger.warning(f"No mock data found for tutorial ID: {tutorial_id}")
                return None
                
            # Handle not found
            if response.status_code == 404:
                logger.warning(f"Tutorial with ID '{tutorial_id}' not found")
                console.print(f"[yellow]Tutorial with ID '{tutorial_id}' not found[/yellow]")
                return None
                
            response.raise_for_status()  # Raise exception for other 4XX/5XX responses
            
            tutorial = response.json()
            logger.info(f"Successfully fetched tutorial: {tutorial.get('title', 'Unknown')}")
            return tutorial
            
        except httpx.TimeoutException:
            logger.error(f"Request timed out while fetching tutorial {tutorial_id}")
            console.print("[red]Request timed out. Please try again later.[/red]")
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP Error: {e.response.status_code} - {e.response.reason_phrase}")
            console.print(f"[red]Error: {e.response.reason_phrase}[/red]")
            
        except Exception as e:
            logger.error(f"Unexpected error fetching tutorial {tutorial_id}: {str(e)}")
            console.print(f"[red]Error connecting to CmdShiftLearn API: {str(e)}[/red]")
        
        # Return mock tutorial if it exists in our fallback data and USE_MOCK_DATA is True
        if USE_MOCK_DATA:
            logger.info(f"Attempting to use mock data for tutorial {tutorial_id}")
            for tutorial in MOCK_TUTORIALS:
                if tutorial["id"] == tutorial_id:
                    logger.info(f"Returning mock data for tutorial {tutorial_id}")
                    return tutorial
                    
            logger.warning(f"No mock data available for tutorial {tutorial_id}")
        
        return None
        
    def complete_tutorial(self, tutorial_id: str, xp_earned: int) -> bool:
        """
        Report tutorial completion to the API.
        
        Args:
            tutorial_id: The ID of the completed tutorial
            xp_earned: The amount of XP earned
            
        Returns:
            bool: True if successful, False otherwise
        """
        url = f"{API_BASE_URL}/progress/tutorial-complete"
        logger.info(f"Reporting completion of tutorial {tutorial_id} to {url}")
        
        try:
            # Get authentication headers with API key
            headers = get_auth_header(self.api_key)
            
            # Prepare the payload
            payload = {
                "tutorialId": tutorial_id,
                "xpEarned": xp_earned
            }
            
            # Use httpx with a 10-second timeout
            with httpx.Client(timeout=10.0) as client:
                response = client.post(url, headers=headers, json=payload)
            
            logger.info(f"API response status code: {response.status_code}")
            
            # Handle authentication failure
            if response.status_code == 401:
                logger.error(f"Authentication failed. Response: {response.text}")
                console.print("[red]Authentication failed: Invalid API key[/red]")
                return False
                
            response.raise_for_status()  # Raise exception for other 4XX/5XX responses
            
            logger.info(f"Successfully reported completion of tutorial {tutorial_id}")
            return True
            
        except Exception as e:
            logger.error(f"Error reporting tutorial completion: {str(e)}")
            console.print(f"[yellow]Could not save progress: {str(e)}[/yellow]")
            return False