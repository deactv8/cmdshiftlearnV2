#!/usr/bin/env python3
"""
Test script specifically for the powershell-basics-3 tutorial
"""

import logging
from api.auth import get_auth_header
from api.tutorials import TutorialClient

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('test_powershell_basics_3')

def test_powershell_basics_3_tutorial():
    """Test retrieving and parsing the powershell-basics-3 tutorial"""
    
    # Use the API key directly
    api_key = "devkey123"
    
    # Test fetching the specific tutorial
    print("\nTesting retrieval of the powershell-basics-3 tutorial...")
    client = TutorialClient(api_key)
    tutorial = client.get_tutorial_by_id("powershell-basics-3")
    
    if tutorial:
        print(f"Successfully retrieved tutorial: {tutorial.get('title')}")
        print(f"Tutorial ID: {tutorial.get('id')}")
        print(f"Description: {tutorial.get('description')}")
        print(f"XP: {tutorial.get('xp')}")
        print(f"Difficulty: {tutorial.get('difficulty')}")
        
        # Check the content
        content = tutorial.get('content')
        print(f"Content length: {len(content or '')}")
        if content:
            print(f"Content preview: {content[:100]}...")
        else:
            print("No content found!")
        
        # Check for steps
        steps = tutorial.get('steps', [])
        print(f"Tutorial has {len(steps)} steps")
        
        # Examine steps in detail
        for i, step in enumerate(steps, 1):
            print(f"\nStep {i}:")
            print(f"  ID: {step.get('id')}")
            print(f"  Title: {step.get('title')}")
            print(f"  Instructions length: {len(step.get('instructions', ''))}")
            print(f"  Expected Command: {step.get('expectedCommand')}")
            print(f"  Hint available: {'Yes' if step.get('hint') else 'No'}")
            
            # Check for validation
            validation = step.get('validation')
            if validation:
                print(f"  Validation: Type={validation.get('type')}, Value={validation.get('value')}")
            
        # Test progress reporting
        print("\nTesting progress reporting...")
        success = client.complete_tutorial(tutorial.get('id'), 10)
        print(f"Progress reporting successful: {success}")
    else:
        print(f"Failed to retrieve powershell-basics-3 tutorial")

if __name__ == "__main__":
    test_powershell_basics_3_tutorial()
