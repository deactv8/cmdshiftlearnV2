#!/usr/bin/env python3
"""
Test all available tutorials from the API
"""

import logging
from api.auth import get_auth_header
from api.tutorials import TutorialClient

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('test_all_tutorials')

def test_all_tutorials():
    """Test retrieving and parsing all available tutorials"""
    
    # Use the API key directly
    api_key = "devkey123"
    
    # Create tutorial client with the API key
    client = TutorialClient(api_key)
    
    # Fetch the list of tutorials
    print("\nFetching all tutorials...")
    tutorials = client.get_tutorials()
    
    if not tutorials:
        print("Failed to retrieve tutorials. Authentication may have failed.")
        return
        
    print(f"Successfully retrieved {len(tutorials)} tutorials:")
    for i, tutorial in enumerate(tutorials, 1):
        print(f"{i}. {tutorial.get('id')} - {tutorial.get('title')}")
    
    # For each tutorial, test retrieving the full details
    print("\nTesting each tutorial...")
    
    success_count = 0
    failure_count = 0
    
    for tutorial_meta in tutorials:
        tutorial_id = tutorial_meta.get('id')
        print(f"\nTesting tutorial: {tutorial_id}")
        
        # Get the full tutorial
        tutorial = client.get_tutorial_by_id(tutorial_id)
        
        if tutorial:
            success_count += 1
            print(f"SUCCESS: Retrieved tutorial: {tutorial.get('title')}")
            
            # Check for steps
            steps = tutorial.get('steps', [])
            print(f"  Tutorial has {len(steps)} steps")
            
            # Check if we have a valid tutorial structure
            is_valid = (
                tutorial.get('id') and
                tutorial.get('title') and
                tutorial.get('description') and
                len(steps) > 0
            )
            
            if is_valid:
                print(f"  SUCCESS: Tutorial has valid structure")
                
                # Test progress reporting
                success = client.complete_tutorial(tutorial_id, 10)
                if success:
                    print(f"  SUCCESS: Progress reporting successful")
                else:
                    print(f"  FAILURE: Progress reporting failed")
            else:
                print(f"  FAILURE: Tutorial is missing required fields:")
                print(f"    ID: {'Present' if tutorial.get('id') else 'Missing'}")
                print(f"    Title: {'Present' if tutorial.get('title') else 'Missing'}")
                print(f"    Description: {'Present' if tutorial.get('description') else 'Missing'}")
                print(f"    Steps: {'Present' if len(steps) > 0 else 'Missing'}")
        else:
            failure_count += 1
            print(f"FAILURE: Failed to retrieve tutorial: {tutorial_id}")
    
    # Print summary
    print("\n=== Summary ===")
    print(f"Total tutorials: {len(tutorials)}")
    print(f"Successfully retrieved: {success_count}")
    print(f"Failed to retrieve: {failure_count}")
    
    if failure_count == 0:
        print("\nSUCCESS: All tutorials are working correctly!")
    else:
        print(f"\nFAILURE: {failure_count} tutorials failed to load properly.")

if __name__ == "__main__":
    test_all_tutorials()
