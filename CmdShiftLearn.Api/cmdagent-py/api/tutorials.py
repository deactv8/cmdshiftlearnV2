"""
API client for interacting with the CmdShiftLearn tutorials API.
"""

import json
import logging
import httpx
from typing import List, Dict, Any, Optional
from rich.console import Console

# Import only what's needed
from utils import API_BASE_URL
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
                return self._load_local_tutorials_fallback()
                
            response.raise_for_status()  # Raise exception for other 4XX/5XX responses
            
            tutorials = response.json()
            logger.info(f"Successfully fetched {len(tutorials)} tutorials")
            
            # If no tutorials were found or response is empty, try local fallback
            if not tutorials:
                logger.warning("No tutorials returned from API, trying local fallback")
                return self._load_local_tutorials_fallback()
                
            return tutorials
            
        except httpx.TimeoutException:
            logger.error("Request timed out while fetching tutorials")
            console.print("[red]Request timed out. Falling back to local tutorials.[/red]")
            return self._load_local_tutorials_fallback()
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error occurred: {e.response.status_code} - {e.response.reason_phrase}")
            console.print(f"[red]Error: {e.response.reason_phrase}. Falling back to local tutorials.[/red]")
            return self._load_local_tutorials_fallback()
            
        except Exception as e:
            logger.error(f"Unexpected error: {str(e)}")
            console.print(f"[red]Error connecting to CmdShiftLearn API: {str(e)}. Falling back to local tutorials.[/red]")
            return self._load_local_tutorials_fallback()
            
    def _load_local_tutorials_fallback(self) -> List[Dict[str, Any]]:
        """
        Load tutorials from local files as a fallback when API is unavailable.
        
        Returns:
            List[Dict[str, Any]]: A list of tutorial objects loaded from local files
        """
        logger.info("Loading tutorials from local files as fallback")
        console.print("[yellow]Loading tutorials from local files as fallback...[/yellow]")
        
        # Import needed modules
        from pathlib import Path
        import os
        import yaml
        
        tutorials = []
        
        try:
            # Build the path to the local tutorials directory
            local_path = Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials" / "beginner"
            
            logger.info(f"Looking for tutorial files in: {local_path}")
            
            if not local_path.exists():
                logger.error(f"Local tutorials directory not found: {local_path}")
                console.print(f"[red]Local tutorials directory not found: {local_path}[/red]")
                return []
            
            # Find all YAML files in the directory
            yaml_files = list(local_path.glob("*.yaml")) + list(local_path.glob("*.yml"))
            
            logger.info(f"Found {len(yaml_files)} local tutorial files")
            
            # Load each tutorial file
            for yaml_file in yaml_files:
                try:
                    with open(yaml_file, 'r', encoding='utf-8') as f:
                        # Load YAML content
                        tutorial_data = yaml.safe_load(f)
                        
                        if tutorial_data and isinstance(tutorial_data, dict):
                            # Convert to tutorial metadata format
                            tutorial_metadata = {
                                "id": tutorial_data.get("id", yaml_file.stem),
                                "title": tutorial_data.get("title", "Untitled Tutorial"),
                                "description": tutorial_data.get("description", ""),
                                "difficulty": tutorial_data.get("difficulty", "Beginner"),
                                "xp": tutorial_data.get("xpTotal", 0),
                                "fromLocalFile": True,  # Mark as loaded from local file
                                "localPath": str(yaml_file)  # Include path for later reference
                            }
                            
                            tutorials.append(tutorial_metadata)
                            logger.info(f"Added local tutorial: {tutorial_metadata['title']} (ID: {tutorial_metadata['id']})")
                            
                except Exception as e:
                    logger.error(f"Error loading local tutorial file {yaml_file}: {str(e)}")
            
            logger.info(f"Loaded {len(tutorials)} tutorials from local files")
            
            if tutorials:
                console.print(f"[green]Loaded {len(tutorials)} tutorials from local files[/green]")
            else:
                console.print("[yellow]No local tutorial files found[/yellow]")
            
            return tutorials
            
        except Exception as e:
            logger.error(f"Error loading local tutorials: {str(e)}")
            console.print(f"[red]Error loading local tutorials: {str(e)}[/red]")
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
                return None
                
            # Handle not found
            if response.status_code == 404:
                logger.warning(f"Tutorial with ID '{tutorial_id}' not found")
                console.print(f"[yellow]Tutorial with ID '{tutorial_id}' not found[/yellow]")
                
                # Try to load a tutorial from local files if available
                from pathlib import Path
                import os
                import yaml
                
                logger.info(f"Attempting to load tutorial {tutorial_id} from local files as fallback")
                local_path = Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials" / "beginner"
                
                # Try both with and without extension
                potential_paths = [
                    local_path / f"{tutorial_id}.yaml",
                    local_path / f"{tutorial_id}.yml",
                    local_path / tutorial_id,
                ]
                
                for path in potential_paths:
                    if path.exists() and path.is_file():
                        logger.info(f"Found local tutorial file: {path}")
                        try:
                            with open(path, 'r', encoding='utf-8') as f:
                                # Load YAML content
                                tutorial_data = yaml.safe_load(f)
                                if tutorial_data and isinstance(tutorial_data, dict):
                                    logger.info(f"Successfully loaded local tutorial: {tutorial_data.get('title', 'Unknown')}")
                                    console.print(f"[green]Loaded tutorial from local file: {path.name}[/green]")
                                    return tutorial_data
                        except Exception as local_err:
                            logger.error(f"Error loading local tutorial file: {str(local_err)}")
                
                # If we reach here, no local file was found or loaded
                logger.warning(f"No local fallback found for tutorial ID: {tutorial_id}")
                return None
                
            response.raise_for_status()  # Raise exception for other 4XX/5XX responses
            
            tutorial = response.json()
            logger.info(f"Successfully fetched tutorial: {tutorial.get('title', 'Unknown')}")
            return tutorial
            
        except httpx.TimeoutException:
            logger.error(f"Request timed out while fetching tutorial {tutorial_id}")
            console.print("[red]Request timed out. Please try again later.[/red]")
            
            # Try local fallback
            console.print("[yellow]Attempting to load from local files...[/yellow]")
            # Recursively call the same function but with a flag to only check local files
            from pathlib import Path
            import os
            import yaml
            
            logger.info(f"Attempting to load tutorial {tutorial_id} from local files as fallback after timeout")
            local_path = Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials" / "beginner"
            
            # Try both with and without extension
            potential_paths = [
                local_path / f"{tutorial_id}.yaml",
                local_path / f"{tutorial_id}.yml",
                local_path / tutorial_id,
            ]
            
            for path in potential_paths:
                if path.exists() and path.is_file():
                    logger.info(f"Found local tutorial file: {path}")
                    try:
                        with open(path, 'r', encoding='utf-8') as f:
                            # Load YAML content
                            tutorial_data = yaml.safe_load(f)
                            if tutorial_data and isinstance(tutorial_data, dict):
                                logger.info(f"Successfully loaded local tutorial: {tutorial_data.get('title', 'Unknown')}")
                                console.print(f"[green]Loaded tutorial from local file: {path.name}[/green]")
                                return tutorial_data
                    except Exception as local_err:
                        logger.error(f"Error loading local tutorial file: {str(local_err)}")
            
            return None
            
        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP Error: {e.response.status_code} - {e.response.reason_phrase}")
            console.print(f"[red]Error: {e.response.reason_phrase}[/red]")
            return None
            
        except Exception as e:
            logger.error(f"Unexpected error fetching tutorial {tutorial_id}: {str(e)}")
            console.print(f"[red]Error connecting to CmdShiftLearn API: {str(e)}[/red]")
            
            # Try local fallback as a last resort
            console.print("[yellow]Attempting to load from local files...[/yellow]")
            from pathlib import Path
            import os
            import yaml
            
            logger.info(f"Attempting to load tutorial {tutorial_id} from local files as last resort")
            local_path = Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials" / "beginner"
            
            # Try both with and without extension
            potential_paths = [
                local_path / f"{tutorial_id}.yaml",
                local_path / f"{tutorial_id}.yml",
                local_path / tutorial_id,
            ]
            
            for path in potential_paths:
                if path.exists() and path.is_file():
                    logger.info(f"Found local tutorial file: {path}")
                    try:
                        with open(path, 'r', encoding='utf-8') as f:
                            # Load YAML content
                            tutorial_data = yaml.safe_load(f)
                            if tutorial_data and isinstance(tutorial_data, dict):
                                logger.info(f"Successfully loaded local tutorial: {tutorial_data.get('title', 'Unknown')}")
                                console.print(f"[green]Loaded tutorial from local file: {path.name}[/green]")
                                return tutorial_data
                    except Exception as local_err:
                        logger.error(f"Error loading local tutorial file: {str(local_err)}")
            
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