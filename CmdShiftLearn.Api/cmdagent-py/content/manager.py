"""
Content manager for handling tutorials, challenges, and certification content.
"""

import os
from typing import Dict, List, Any, Optional, Tuple

from content.repository import ContentRepository


class ContentManager:
    """Manage tutorials, challenges, and certification content."""
    
    def __init__(self, content_repository: ContentRepository = None):
        """
        Initialize the content manager.
        
        Args:
            content_repository: Repository for content storage
        """
        self.repository = content_repository or ContentRepository()
        self.repository.load_all_content()
    
    def get_tutorial_list(self, difficulty: str = None, certification: str = None) -> List[Dict[str, Any]]:
        """
        Get a list of tutorials, optionally filtered by difficulty or certification.
        
        Args:
            difficulty: Optional difficulty filter (beginner, intermediate, advanced)
            certification: Optional certification ID filter
            
        Returns:
            list: List of tutorials
        """
        if difficulty:
            tutorials = self.repository.get_tutorials_by_difficulty(difficulty)
        elif certification:
            tutorials = self.repository.get_tutorials_by_certification(certification)
        else:
            tutorials = self.repository.get_all_tutorials()
        
        # Sort tutorials by difficulty and then by title
        difficulty_order = {'beginner': 0, 'intermediate': 1, 'advanced': 2}
        
        return sorted(
            tutorials,
            key=lambda t: (
                difficulty_order.get(t.get('difficulty', 'beginner').lower(), 0),
                t.get('title', '')
            )
        )
    
    def get_challenge_list(self, difficulty: str = None) -> List[Dict[str, Any]]:
        """
        Get a list of challenges, optionally filtered by difficulty.
        
        Args:
            difficulty: Optional difficulty filter (beginner, intermediate, advanced)
            
        Returns:
            list: List of challenges
        """
        challenges = self.repository.get_all_challenges()
        
        if difficulty:
            challenges = [c for c in challenges 
                         if c.get('difficulty', '').lower() == difficulty.lower()]
        
        # Sort challenges by difficulty and then by title
        difficulty_order = {'beginner': 0, 'intermediate': 1, 'advanced': 2}
        
        return sorted(
            challenges,
            key=lambda c: (
                difficulty_order.get(c.get('difficulty', 'beginner').lower(), 0),
                c.get('title', '')
            )
        )
    
    def get_certification_list(self) -> List[Dict[str, Any]]:
        """
        Get a list of all certifications.
        
        Returns:
            list: List of certifications
        """
        return sorted(
            self.repository.get_all_certifications(),
            key=lambda c: c.get('title', '')
        )
    
    def get_tutorial(self, tutorial_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a tutorial by ID.
        
        Args:
            tutorial_id: The tutorial ID
            
        Returns:
            dict: The tutorial data or None if not found
        """
        return self.repository.get_tutorial(tutorial_id)
    
    def get_challenge(self, challenge_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a challenge by ID.
        
        Args:
            challenge_id: The challenge ID
            
        Returns:
            dict: The challenge data or None if not found
        """
        return self.repository.get_challenge(challenge_id)
    
    def get_certification(self, cert_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a certification by ID.
        
        Args:
            cert_id: The certification ID
            
        Returns:
            dict: The certification data or None if not found
        """
        return self.repository.get_certification(cert_id)
    
    def get_tutorial_step(self, tutorial_id: str, step_index: int) -> Optional[Dict[str, Any]]:
        """
        Get a specific step from a tutorial.
        
        Args:
            tutorial_id: The tutorial ID
            step_index: The step index (0-based)
            
        Returns:
            dict: The step data or None if not found
        """
        tutorial = self.repository.get_tutorial(tutorial_id)
        if not tutorial:
            return None
            
        steps = tutorial.get('steps', [])
        if not steps or step_index < 0 or step_index >= len(steps):
            return None
            
        return steps[step_index]
    
    def get_related_tutorials(self, tutorial_id: str) -> List[Dict[str, Any]]:
        """
        Get tutorials related to the specified tutorial.
        
        Args:
            tutorial_id: The tutorial ID
            
        Returns:
            list: List of related tutorials
        """
        tutorial = self.repository.get_tutorial(tutorial_id)
        if not tutorial:
            return []
            
        # Get tutorials with similar topics
        topics = tutorial.get('topics', [])
        difficulty = tutorial.get('difficulty', 'beginner')
        
        # Find tutorials with similar topics
        related = []
        for other_id, other in self.repository.tutorials.items():
            if other_id == tutorial_id:
                continue
                
            # Check for overlapping topics
            other_topics = other.get('topics', [])
            common_topics = set(topics).intersection(set(other_topics))
            
            if common_topics:
                # Calculate a relevance score
                relevance = len(common_topics) / max(len(topics), len(other_topics))
                
                # Bonus for same difficulty level
                if other.get('difficulty') == difficulty:
                    relevance += 0.2
                    
                related.append((other, relevance))
        
        # Sort by relevance and take the top 5
        related.sort(key=lambda x: x[1], reverse=True)
        return [item[0] for item in related[:5]]
    
    def get_tutorial_path(self, user_level: int = 1) -> List[Dict[str, Any]]:
        """
        Get a recommended path of tutorials based on user level.
        
        Args:
            user_level: The user's current level
            
        Returns:
            list: List of recommended tutorials
        """
        # Start with beginner tutorials for low levels
        if user_level < 5:
            difficulty = "beginner"
        elif user_level < 10:
            difficulty = "intermediate"
        else:
            difficulty = "advanced"
            
        # Get tutorials of appropriate difficulty
        tutorials = self.repository.get_tutorials_by_difficulty(difficulty)
        
        # Sort by tutorial level (if available) or default to alphabetical
        return sorted(
            tutorials,
            key=lambda t: (t.get('level', 1), t.get('title', ''))
        )
    
    def validate_tutorial_completion(self, tutorial_id: str, user_answers: List[str]) -> Tuple[bool, List[bool], int]:
        """
        Validate a user's answers for a tutorial.
        
        Args:
            tutorial_id: The tutorial ID
            user_answers: List of user's answers for each step
            
        Returns:
            tuple: (overall_success, step_results, score)
                overall_success: True if all required steps passed
                step_results: List of booleans indicating success for each step
                score: Score out of 100
        """
        tutorial = self.repository.get_tutorial(tutorial_id)
        if not tutorial or not user_answers:
            return False, [], 0
            
        steps = tutorial.get('steps', [])
        if not steps:
            return False, [], 0
            
        # Match answers with steps
        results = []
        for i, (step, answer) in enumerate(zip(steps, user_answers)):
            # Skip non-interactive steps
            if step.get('type') != 'command' and step.get('type') != 'challenge':
                results.append(True)
                continue
                
            # Check if the answer matches the expected command
            expected_command = step.get('command', '') or step.get('expected_command', '')
            
            # Simple exact match for now (could be enhanced with fuzzy matching)
            results.append(answer.strip().lower() == expected_command.strip().lower())
        
        # Calculate score (percentage of correct answers)
        score = int((sum(results) / len(results)) * 100) if results else 0
        
        # Overall success if all required steps passed
        required_steps = [i for i, step in enumerate(steps) 
                         if step.get('required', True) and 
                         (step.get('type') == 'command' or step.get('type') == 'challenge')]
                         
        overall_success = all(results[i] for i in required_steps) if required_steps else True
        
        return overall_success, results, score
    
    def create_tutorial(self, tutorial_data: Dict[str, Any]) -> bool:
        """
        Create a new tutorial.
        
        Args:
            tutorial_data: The tutorial data
            
        Returns:
            bool: True if created successfully, False otherwise
        """
        # Make sure required fields are present
        required_fields = ['id', 'title', 'description', 'difficulty']
        if not all(field in tutorial_data for field in required_fields):
            return False
            
        # Add default values if not provided
        tutorial_data.setdefault('steps', [])
        tutorial_data.setdefault('topics', [])
        tutorial_data.setdefault('xp_reward', 100)
        
        # Save the tutorial
        return self.repository.save_tutorial(tutorial_data)
    
    def update_tutorial(self, tutorial_id: str, updated_data: Dict[str, Any]) -> bool:
        """
        Update an existing tutorial.
        
        Args:
            tutorial_id: The tutorial ID to update
            updated_data: The updated tutorial data
            
        Returns:
            bool: True if updated successfully, False otherwise
        """
        # Get the existing tutorial
        tutorial = self.repository.get_tutorial(tutorial_id)
        if not tutorial:
            return False
            
        # Update the tutorial data
        tutorial.update(updated_data)
        
        # Save the updated tutorial
        return self.repository.save_tutorial(tutorial)
    
    def delete_tutorial(self, tutorial_id: str) -> bool:
        """
        Delete a tutorial.
        
        Args:
            tutorial_id: The tutorial ID to delete
            
        Returns:
            bool: True if deleted successfully, False otherwise
        """
        return self.repository.delete_tutorial(tutorial_id)
