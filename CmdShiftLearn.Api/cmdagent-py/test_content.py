"""
Test script to load content from local files.
"""

from content.repository import ContentRepository
import os

def main():
    print("==== Testing content loading from local files ====")
    
    # Create content repository
    repo = ContentRepository()
    
    # Load all content
    print("\nLoading content...")
    repo.load_all_content()
    
    # Check tutorials
    tutorials = repo.get_all_tutorials()
    print(f"\nLoaded {len(tutorials)} tutorials:")
    for tutorial in tutorials:
        print(f"  - {tutorial.get('title', 'Unknown')} ({tutorial.get('id', 'Unknown')}) - {tutorial.get('difficulty', 'Unknown')}")
    
    # Check beginner tutorials specifically 
    beginner_tutorials = repo.get_tutorials_by_difficulty("beginner")
    print(f"\nLoaded {len(beginner_tutorials)} beginner tutorials:")
    for tutorial in beginner_tutorials:
        print(f"  - {tutorial.get('title', 'Unknown')} ({tutorial.get('id', 'Unknown')})")
    
    # Check challenges
    challenges = repo.get_all_challenges()
    print(f"\nLoaded {len(challenges)} challenges:")
    for challenge in challenges:
        print(f"  - {challenge.get('title', 'Unknown')} ({challenge.get('id', 'Unknown')}) - {challenge.get('difficulty', 'Unknown')}")
    
    # Check tutorial files in directory
    tutorial_dir = os.path.join(repo.content_dir, "tutorials")
    tutorial_files = []
    for root, dirs, files in os.walk(tutorial_dir):
        for file in files:
            if file.endswith(".yaml"):
                tutorial_files.append(os.path.join(root, file))
    
    print(f"\nFound {len(tutorial_files)} tutorial files:")
    for file in tutorial_files:
        print(f"  - {file}")
    
    # Check challenge files in directory
    challenge_dir = os.path.join(repo.content_dir, "challenges")
    challenge_files = []
    for root, dirs, files in os.walk(challenge_dir):
        for file in files:
            if file.endswith(".yaml"):
                challenge_files.append(os.path.join(root, file))
    
    print(f"\nFound {len(challenge_files)} challenge files:")
    for file in challenge_files:
        print(f"  - {file}")
    
    print("\n==== Test completed! ====")

if __name__ == "__main__":
    main()
