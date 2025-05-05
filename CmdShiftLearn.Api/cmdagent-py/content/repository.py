"""
Repository for managing tutorial and challenge content.
"""

import os
import yaml
from pathlib import Path
from typing import Dict, List, Any, Optional

from utils.config import DATA_DIR


class ContentRepository:
    """Repository for loading and managing tutorial and challenge content."""
    
    def __init__(self, content_dir: str = None):
        """
        Initialize the content repository.
        
        Args:
            content_dir: Directory containing content files (defaults to data/content)
        """
        self.content_dir = Path(content_dir) if content_dir else Path(DATA_DIR) / "content"
        self.tutorials = {}
        self.challenges = {}
        self.certifications = {}
        self.skill_trees = {}
    
    def load_all_content(self):
        """Load all content from the content directory."""
        self._load_tutorials()
        self._load_challenges()
        self._load_certifications()
        self._load_skill_trees()
        
        # Build relationships between content items
        self._build_content_relationships()
    
    def _load_tutorials(self):
        """Load tutorials from YAML files."""
        tutorial_dir = self.content_dir / "tutorials"
        if not tutorial_dir.exists():
            os.makedirs(tutorial_dir, exist_ok=True)
            return
        
        # Load YAML files from tutorial_dir and all subdirectories
        # Use set() to prevent duplicate loading
        yaml_files = set()
        yaml_files.update(tutorial_dir.glob("*.yaml"))
        yaml_files.update(tutorial_dir.glob("*/*.yaml"))
        
        for file_path in yaml_files:
            try:
                with open(file_path, 'r') as file:
                    tutorial_data = yaml.safe_load(file)
                    if tutorial_data and 'id' in tutorial_data:
                        self.tutorials[tutorial_data['id']] = tutorial_data
                        print(f"Loaded tutorial: {tutorial_data.get('title', 'Unknown')} ({file_path})")
            except Exception as e:
                print(f"Error loading tutorial from {file_path}: {e}")
    
    def _load_challenges(self):
        """Load challenges from YAML files."""
        challenge_dir = self.content_dir / "challenges"
        if not challenge_dir.exists():
            os.makedirs(challenge_dir, exist_ok=True)
            return
            
        # Load YAML files from challenge_dir and all subdirectories
        # Use set() to prevent duplicate loading
        yaml_files = set()
        yaml_files.update(challenge_dir.glob("*.yaml"))
        yaml_files.update(challenge_dir.glob("*/*.yaml"))
        
        for file_path in yaml_files:
            try:
                with open(file_path, 'r') as file:
                    challenge_data = yaml.safe_load(file)
                    if challenge_data and 'id' in challenge_data:
                        self.challenges[challenge_data['id']] = challenge_data
                        print(f"Loaded challenge: {challenge_data.get('title', 'Unknown')} ({file_path})")
            except Exception as e:
                print(f"Error loading challenge from {file_path}: {e}")
    
    def _load_certifications(self):
        """Load certification mappings from YAML files."""
        cert_dir = self.content_dir / "certifications"
        if not cert_dir.exists():
            os.makedirs(cert_dir, exist_ok=True)
            return
            
        for file_path in cert_dir.glob("*.yaml"):
            try:
                with open(file_path, 'r') as file:
                    cert_data = yaml.safe_load(file)
                    if cert_data and 'id' in cert_data:
                        self.certifications[cert_data['id']] = cert_data
            except Exception as e:
                print(f"Error loading certification from {file_path}: {e}")
    
    def _load_skill_trees(self):
        """Load skill trees from YAML files."""
        skill_tree_dir = self.content_dir / "skill_trees"
        if not skill_tree_dir.exists():
            os.makedirs(skill_tree_dir, exist_ok=True)
            return
            
        for file_path in skill_tree_dir.glob("*.yaml"):
            try:
                with open(file_path, 'r') as file:
                    skill_tree_data = yaml.safe_load(file)
                    if skill_tree_data and 'id' in skill_tree_data:
                        self.skill_trees[skill_tree_data['id']] = skill_tree_data
            except Exception as e:
                print(f"Error loading skill tree from {file_path}: {e}")
    
    def _build_content_relationships(self):
        """Build relationships between content items (prerequisites, related content, etc.)."""
        # Link tutorials to their certifications
        for tutorial_id, tutorial in self.tutorials.items():
            cert_mappings = tutorial.get('certification_mappings', [])
            for mapping in cert_mappings:
                cert_id = mapping.get('cert_id')
                if cert_id and cert_id in self.certifications:
                    # Add this tutorial to the certification's tutorial list
                    if 'tutorials' not in self.certifications[cert_id]:
                        self.certifications[cert_id]['tutorials'] = []
                    
                    # Only add if not already in the list
                    if tutorial_id not in self.certifications[cert_id]['tutorials']:
                        self.certifications[cert_id]['tutorials'].append(tutorial_id)
        
        # Link challenges to their tutorials and certifications
        for challenge_id, challenge in self.challenges.items():
            related_tutorials = challenge.get('related_tutorials', [])
            for tutorial_id in related_tutorials:
                if tutorial_id in self.tutorials:
                    # Add this challenge to the tutorial's challenge list
                    if 'related_challenges' not in self.tutorials[tutorial_id]:
                        self.tutorials[tutorial_id]['related_challenges'] = []
                    
                    # Only add if not already in the list
                    if challenge_id not in self.tutorials[tutorial_id]['related_challenges']:
                        self.tutorials[tutorial_id]['related_challenges'].append(challenge_id)
    
    def get_tutorial(self, tutorial_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a tutorial by ID.
        
        Args:
            tutorial_id: The tutorial ID
            
        Returns:
            dict: The tutorial data or None if not found
        """
        return self.tutorials.get(tutorial_id)
    
    def get_all_tutorials(self) -> List[Dict[str, Any]]:
        """
        Get all tutorials.
        
        Returns:
            list: List of all tutorials
        """
        return list(self.tutorials.values())
    
    def get_tutorials_by_difficulty(self, difficulty: str) -> List[Dict[str, Any]]:
        """
        Get tutorials by difficulty level.
        
        Args:
            difficulty: The difficulty level (beginner, intermediate, advanced)
            
        Returns:
            list: List of tutorials with the specified difficulty
        """
        return [tutorial for tutorial in self.tutorials.values() 
                if tutorial.get('difficulty', '').lower() == difficulty.lower()]
    
    def get_tutorials_by_certification(self, cert_id: str) -> List[Dict[str, Any]]:
        """
        Get tutorials related to a certification.
        
        Args:
            cert_id: The certification ID
            
        Returns:
            list: List of tutorials related to the certification
        """
        cert = self.certifications.get(cert_id, {})
        tutorial_ids = cert.get('tutorials', [])
        
        return [self.tutorials[tutorial_id] for tutorial_id in tutorial_ids 
                if tutorial_id in self.tutorials]
    
    def get_challenge(self, challenge_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a challenge by ID.
        
        Args:
            challenge_id: The challenge ID
            
        Returns:
            dict: The challenge data or None if not found
        """
        return self.challenges.get(challenge_id)
    
    def get_all_challenges(self) -> List[Dict[str, Any]]:
        """
        Get all challenges.
        
        Returns:
            list: List of all challenges
        """
        return list(self.challenges.values())
    
    def get_certification(self, cert_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a certification by ID.
        
        Args:
            cert_id: The certification ID
            
        Returns:
            dict: The certification data or None if not found
        """
        return self.certifications.get(cert_id)
    
    def get_all_certifications(self) -> List[Dict[str, Any]]:
        """
        Get all certifications.
        
        Returns:
            list: List of all certifications
        """
        return list(self.certifications.values())
    
    def get_skill_tree(self, tree_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a skill tree by ID.
        
        Args:
            tree_id: The skill tree ID
            
        Returns:
            dict: The skill tree data or None if not found
        """
        return self.skill_trees.get(tree_id)
    
    def get_related_challenges(self, tutorial_id: str) -> List[Dict[str, Any]]:
        """
        Get challenges related to a tutorial.
        
        Args:
            tutorial_id: The tutorial ID
            
        Returns:
            list: List of challenges related to the tutorial
        """
        tutorial = self.tutorials.get(tutorial_id, {})
        challenge_ids = tutorial.get('related_challenges', [])
        
        return [self.challenges[challenge_id] for challenge_id in challenge_ids 
                if challenge_id in self.challenges]
    
    def save_tutorial(self, tutorial_data: Dict[str, Any]) -> bool:
        """
        Save a tutorial to the repository.
        
        Args:
            tutorial_data: The tutorial data to save
            
        Returns:
            bool: True if saved successfully, False otherwise
        """
        if not tutorial_data or 'id' not in tutorial_data:
            return False
        
        tutorial_id = tutorial_data['id']
        file_path = self.content_dir / "tutorials" / f"{tutorial_id}.yaml"
        
        try:
            # Make sure the directory exists
            os.makedirs(os.path.dirname(file_path), exist_ok=True)
            
            # Save the tutorial data
            with open(file_path, 'w') as file:
                yaml.dump(tutorial_data, file, default_flow_style=False)
            
            # Update in-memory data
            self.tutorials[tutorial_id] = tutorial_data
            
            return True
        except Exception as e:
            print(f"Error saving tutorial: {e}")
            return False
    
    def delete_tutorial(self, tutorial_id: str) -> bool:
        """
        Delete a tutorial from the repository.
        
        Args:
            tutorial_id: The tutorial ID to delete
            
        Returns:
            bool: True if deleted successfully, False otherwise
        """
        if tutorial_id not in self.tutorials:
            return False
        
        file_path = self.content_dir / "tutorials" / f"{tutorial_id}.yaml"
        
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
            
            # Remove from in-memory data
            del self.tutorials[tutorial_id]
            
            return True
        except Exception as e:
            print(f"Error deleting tutorial: {e}")
            return False
