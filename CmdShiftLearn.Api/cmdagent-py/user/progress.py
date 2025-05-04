"""
User progress tracking for CmdShiftLearn.
"""

from typing import Dict, List, Any, Optional, Tuple
from datetime import datetime

from user.profile import UserProfile


class ProgressTracker:
    """Track and manage user progress through tutorials and certifications."""
    
    def __init__(self, user_profile: UserProfile):
        """
        Initialize the progress tracker.
        
        Args:
            user_profile: The user's profile
        """
        self.user_profile = user_profile
    
    def track_tutorial_progress(self, tutorial_id: str, step_index: int, 
                              is_complete: bool = False, score: int = 0) -> Dict[str, Any]:
        """
        Track progress within a tutorial.
        
        Args:
            tutorial_id: Tutorial ID
            step_index: Current step index
            is_complete: Whether the tutorial is completed
            score: Score achieved (if completed)
            
        Returns:
            dict: Progress data
        """
        # Get the progress data structure
        progress_key = f'tutorial_{tutorial_id}'
        progress_data = self._get_progress_data(progress_key)
        
        # Update progress
        progress_data['last_step'] = step_index
        progress_data['last_updated'] = datetime.now().isoformat()
        
        if is_complete:
            progress_data['is_complete'] = True
            progress_data['score'] = score
            progress_data['completed_at'] = datetime.now().isoformat()
            
            # Add to completed tutorials in profile
            xp_earned, new_achievements = self.user_profile.complete_tutorial(tutorial_id, score)
            progress_data['xp_earned'] = xp_earned
            progress_data['new_achievements'] = new_achievements
        
        # Save progress
        self._save_progress_data(progress_key, progress_data)
        
        return progress_data
    
    def track_challenge_progress(self, challenge_id: str, is_complete: bool = False, 
                               score: int = 0) -> Dict[str, Any]:
        """
        Track progress with a challenge.
        
        Args:
            challenge_id: Challenge ID
            is_complete: Whether the challenge is completed
            score: Score achieved (if completed)
            
        Returns:
            dict: Progress data
        """
        # Get the progress data structure
        progress_key = f'challenge_{challenge_id}'
        progress_data = self._get_progress_data(progress_key)
        
        # Update progress
        progress_data['last_updated'] = datetime.now().isoformat()
        
        if is_complete:
            progress_data['is_complete'] = True
            progress_data['score'] = score
            progress_data['completed_at'] = datetime.now().isoformat()
            
            # Add to completed challenges in profile
            xp_earned, new_achievements = self.user_profile.complete_challenge(challenge_id, score)
            progress_data['xp_earned'] = xp_earned
            progress_data['new_achievements'] = new_achievements
        
        # Save progress
        self._save_progress_data(progress_key, progress_data)
        
        return progress_data
    
    def track_certification_progress(self, cert_id: str, domain: str, 
                                   progress_value: float) -> Dict[str, Any]:
        """
        Track progress towards a certification.
        
        Args:
            cert_id: Certification ID
            domain: Certification domain
            progress_value: Progress value (0-100)
            
        Returns:
            dict: Certification progress data
        """
        # Update the profile's certification progress
        self.user_profile.update_certification_progress(cert_id, domain, progress_value)
        
        # Get updated certification progress
        cert_progress = self.user_profile.get_certification_progress(cert_id)
        
        return cert_progress
    
    def get_tutorial_progress(self, tutorial_id: str) -> Dict[str, Any]:
        """
        Get progress data for a tutorial.
        
        Args:
            tutorial_id: Tutorial ID
            
        Returns:
            dict: Tutorial progress data
        """
        progress_key = f'tutorial_{tutorial_id}'
        return self._get_progress_data(progress_key)
    
    def get_challenge_progress(self, challenge_id: str) -> Dict[str, Any]:
        """
        Get progress data for a challenge.
        
        Args:
            challenge_id: Challenge ID
            
        Returns:
            dict: Challenge progress data
        """
        progress_key = f'challenge_{challenge_id}'
        return self._get_progress_data(progress_key)
    
    def get_all_tutorial_progress(self) -> Dict[str, Dict[str, Any]]:
        """
        Get progress data for all tutorials.
        
        Returns:
            dict: Map of tutorial IDs to progress data
        """
        progress_data = {}
        
        # Find all tutorial progress in the user profile
        for tutorial in self.user_profile.completed_tutorials:
            tutorial_id = tutorial.get('id')
            if tutorial_id:
                progress_data[tutorial_id] = self.get_tutorial_progress(tutorial_id)
        
        return progress_data
    
    def get_all_challenge_progress(self) -> Dict[str, Dict[str, Any]]:
        """
        Get progress data for all challenges.
        
        Returns:
            dict: Map of challenge IDs to progress data
        """
        progress_data = {}
        
        # Find all challenge progress in the user profile
        for challenge in self.user_profile.completed_challenges:
            challenge_id = challenge.get('id')
            if challenge_id:
                progress_data[challenge_id] = self.get_challenge_progress(challenge_id)
        
        return progress_data
    
    def get_all_certification_progress(self) -> Dict[str, Dict[str, Any]]:
        """
        Get progress data for all certifications.
        
        Returns:
            dict: Map of certification IDs to progress data
        """
        return self.user_profile.certification_progress
    
    def reset_tutorial_progress(self, tutorial_id: str) -> bool:
        """
        Reset progress for a tutorial.
        
        Args:
            tutorial_id: Tutorial ID
            
        Returns:
            bool: True if reset successful, False otherwise
        """
        progress_key = f'tutorial_{tutorial_id}'
        
        # Create empty progress data
        empty_progress = {
            'last_step': 0,
            'is_complete': False,
            'score': 0,
            'last_updated': datetime.now().isoformat()
        }
        
        # Save empty progress
        return self._save_progress_data(progress_key, empty_progress)
    
    def reset_challenge_progress(self, challenge_id: str) -> bool:
        """
        Reset progress for a challenge.
        
        Args:
            challenge_id: Challenge ID
            
        Returns:
            bool: True if reset successful, False otherwise
        """
        progress_key = f'challenge_{challenge_id}'
        
        # Create empty progress data
        empty_progress = {
            'is_complete': False,
            'score': 0,
            'last_updated': datetime.now().isoformat()
        }
        
        # Save empty progress
        return self._save_progress_data(progress_key, empty_progress)
    
    def calculate_overall_progress(self) -> Dict[str, Any]:
        """
        Calculate overall learning progress.
        
        Returns:
            dict: Overall progress data
        """
        # Get counts
        tutorial_count = self.user_profile.get_tutorial_count()
        challenge_count = self.user_profile.get_challenge_count()
        achievement_count = self.user_profile.get_achievement_count()
        
        # Get certification progress
        cert_progress = self.user_profile.certification_progress
        avg_cert_progress = 0.0
        
        if cert_progress:
            cert_values = [cert.get('total_progress', 0.0) for cert in cert_progress.values()]
            avg_cert_progress = sum(cert_values) / len(cert_values) if cert_values else 0.0
        
        # Calculate overall progress (this can be adjusted based on priorities)
        tutorial_weight = 0.3
        challenge_weight = 0.3
        cert_weight = 0.4
        
        # Normalize counts (assuming targets)
        norm_tutorial = min(1.0, tutorial_count / 20) if tutorial_count else 0.0
        norm_challenge = min(1.0, challenge_count / 15) if challenge_count else 0.0
        norm_cert = avg_cert_progress / 100 if avg_cert_progress else 0.0
        
        overall_progress = (norm_tutorial * tutorial_weight +
                           norm_challenge * challenge_weight +
                           norm_cert * cert_weight) * 100
        
        return {
            'overall_percentage': round(overall_progress, 1),
            'tutorial_completion': norm_tutorial * 100,
            'challenge_completion': norm_challenge * 100,
            'certification_progress': norm_cert * 100,
            'tutorial_count': tutorial_count,
            'challenge_count': challenge_count,
            'achievement_count': achievement_count,
            'level': self.user_profile.level,
            'xp': self.user_profile.xp,
            'next_level_xp': self.user_profile.next_level_xp
        }
    
    def _get_progress_data(self, progress_key: str) -> Dict[str, Any]:
        """
        Get progress data for a key.
        
        Args:
            progress_key: Progress data key
            
        Returns:
            dict: Progress data
        """
        # Progress data is stored in the user's settings
        if 'progress' not in self.user_profile.settings:
            self.user_profile.settings['progress'] = {}
            
        progress_data = self.user_profile.settings['progress'].get(progress_key, {})
        
        # Initialize with defaults if empty
        if not progress_data:
            if progress_key.startswith('tutorial_'):
                progress_data = {
                    'last_step': 0,
                    'is_complete': False,
                    'score': 0,
                    'last_updated': datetime.now().isoformat()
                }
            elif progress_key.startswith('challenge_'):
                progress_data = {
                    'is_complete': False,
                    'score': 0,
                    'last_updated': datetime.now().isoformat()
                }
        
        return progress_data
    
    def _save_progress_data(self, progress_key: str, progress_data: Dict[str, Any]) -> bool:
        """
        Save progress data for a key.
        
        Args:
            progress_key: Progress data key
            progress_data: Progress data to save
            
        Returns:
            bool: True if saved successfully, False otherwise
        """
        # Initialize progress dictionary if needed
        if 'progress' not in self.user_profile.settings:
            self.user_profile.settings['progress'] = {}
            
        # Update progress data
        self.user_profile.settings['progress'][progress_key] = progress_data
        
        # Save profile
        return self.user_profile.save()
