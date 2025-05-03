#!/usr/bin/env python3
"""
Test script for API key authentication
"""

import os
import sys
import logging
from api.auth import get_auth_header, save_api_key, load_api_key
from api.tutorials import TutorialClient

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('test_api_key')

def test_api_key_auth(api_key=None):
    """Test API key authentication with the tutorials endpoint"""
    
    if len(sys.argv) > 1:
        api_key = sys.argv[1]
        print(f"Using API key from command line: {api_key[:3]}***")
    elif api_key:
        print(f"Using provided API key: {api_key[:3]}***")
    else:
        api_key = load_api_key()
        if api_key:
            print(f"Using loaded API key: {api_key[:3]}***")
        else:
            api_key = "devkey123"  # Default test key
            print(f"Using default test key: {api_key}")
    
    # Save the API key for future use
    save_api_key(api_key)
    
    # Get authentication headers
    headers = get_auth_header(api_key)
    print(f"Auth headers: {list(headers.keys())}")
    print(f"Authorization header present: {'Authorization' in headers}")
    
    # Test fetching tutorials with the API key
    print("\nTesting tutorial retrieval with API key...")
    client = TutorialClient(api_key)
    tutorials = client.get_tutorials()
    
    if tutorials:
        print(f"Successfully retrieved {len(tutorials)} tutorials!")
        print("First few tutorials:")
        for i, tutorial in enumerate(tutorials[:3], 1):
            print(f"  {i}. {tutorial.get('id')} - {tutorial.get('title')}")
            
        # Test retrieving a single tutorial
        if tutorials:
            first_tutorial_id = tutorials[0].get('id')
            print(f"\nTesting retrieval of tutorial with ID: {first_tutorial_id}")
            tutorial = client.get_tutorial_by_id(first_tutorial_id)
            
            if tutorial:
                print(f"Successfully retrieved tutorial: {tutorial.get('title')}")
                print(f"Tutorial has {len(tutorial.get('steps', []))} steps")
                
                # Test progress reporting
                print("\nTesting progress reporting...")
                success = client.complete_tutorial(first_tutorial_id, 10)
                print(f"Progress reporting successful: {success}")
            else:
                print(f"Failed to retrieve tutorial with ID: {first_tutorial_id}")
    else:
        print("Failed to retrieve tutorials. Authentication may have failed.")

if __name__ == "__main__":
    test_api_key_auth()
