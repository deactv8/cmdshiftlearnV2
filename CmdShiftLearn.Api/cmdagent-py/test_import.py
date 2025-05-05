"""
Simple test script to verify imports are working.
"""

try:
    print("Testing imports...")
    
    # Test importing from utils.config
    from utils.config import USE_MOCK_DATA
    print(f"Successfully imported USE_MOCK_DATA from utils.config: {USE_MOCK_DATA}")
    
    # Test importing from utils
    from utils import USE_MOCK_DATA as USE_MOCK_DATA2
    print(f"Successfully imported USE_MOCK_DATA from utils: {USE_MOCK_DATA2}")
    
    # Test importing TutorialClient
    from api.tutorials import TutorialClient
    print("Successfully imported TutorialClient from api.tutorials")
    
    # Test importing ChallengeClient
    from api.challenges import ChallengeClient
    print("Successfully imported ChallengeClient from api.challenges")
    
    print("All imports successful!")
except ImportError as e:
    print(f"Import error occurred: {e}")
except Exception as e:
    print(f"Unexpected error: {e}")
