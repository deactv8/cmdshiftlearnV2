"""
User profile management for CmdShiftLearn.
"""

import os
import json
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Any, Optional

from utils.config import DATA_DIR


class UserProfile:
    """User profile management for tracking progress and achievements."""
    
    def __init__(self, username: str, load_data: bool = True):
        """
        Initialize a user profile.
        
        Args:
            username: The username
            load_data: Whether to load existing data for the user
        """
        self.username = username
        self.created_at = datetime.now().isoformat()
        self.last_login = datetime.now().isoformat()
        self.level = 1
        self.xp = 0
        self.next_level_xp = 100  # XP needed for next level
        self.completed_tutorials = []
        self.completed_challenges = []
        self.achievements = []
        self.certification_progress = {}
        self.skill_levels = {}
        self.settings = {
            "theme": "dark",
            "notifications": True,
            "sound_effects": True
        }
        
        # Path to user data file
        self.data_path = Path(DATA_DIR) / "users" / f"{username}.json"
        
        # Load existing data if available
        if load_data:
            self.load()
    
    @classmethod
    def load(cls, username: str) -> Optional['UserProfile']:
        """
        Load a user profile by username.
        
        Args:
            username: The username
            
        Returns:
            UserProfile: The loaded profile or None if not found
        """
        profile = cls(username, load_data=False)
        
        if profile.exists():
            profile.load()
            return profile
        else:
            return None
    
    @classmethod
    def create(cls, username: str) -> 'UserProfile':
        """
        Create a new user profile.
        
        Args:
            username: The username
            
        Returns:
            UserProfile: The created profile
        """
        profile = cls(username, load_data=False)
        profile.save()
        return profile
    
    def exists(self) -> bool:
        """
        Check if the user profile exists.
        
        Returns:
            bool: True if the profile exists, False otherwise
        """
        return self.data_path.exists()
    
    def load(self) -> bool:
        """
        Load user data from disk.
        
        Returns:
            bool: True if loaded successfully, False otherwise
        """
        if not self.data_path.exists():
            return False
            
        try:
            with open(self.data_path, 'r') as file:
                data = json.load(file)
                
                # Copy data to attributes
                self.created_at = data.get('created_at', self.created_at)
                self.last_login = data.get('last_login', self.last_login)
                self.level = data.get('level', self.level)
                self.xp = data.get('xp', self.xp)
                self.next_level_xp = data.get('next_level_xp', self.next_level_xp)
                self.completed_tutorials = data.get('completed_tutorials', self.completed_tutorials)
                self.completed_challenges = data.get('completed_challenges', self.completed_challenges)
                self.achievements = data.get('achievements', self.achievements)
                self.certification_progress = data.get('certification_progress', self.certification_progress)
                self.skill_levels = data.get('skill_levels', self.skill_levels)
                self.settings = data.get('settings', self.settings)
                
                return True
        except Exception as e:
            print(f"Error loading user profile: {e}")
            return False
    
    def save(self) -> bool:
        """
        Save user data to disk.
        
        Returns:
            bool: True if saved successfully, False otherwise
        """
        try:
            # Make sure the directory exists
            os.makedirs(os.path.dirname(self.data_path), exist_ok=True)
            
            # Update last login time
            self.last_login = datetime.now().isoformat()
            
            # Create data dictionary
            data = {
                'username': self.username,
                'created_at': self.created_at,
                'last_login': self.last_login,
                'level': self.level,
                'xp': self.xp,
                'next_level_xp': self.next_level_xp,
                'completed_tutorials': self.completed_tutorials,
                'completed_challenges': self.completed_challenges,
                'achievements': self.achievements,
                'certification_progress': self.certification_progress,
                'skill_levels': self.skill_levels,
                'settings': self.settings
            }
            
            # Save data to file
            with open(self.data_path, 'w') as file:
                json.dump(data, file, indent=2)
                
            return True
        except Exception as e:
            print(f"Error saving user profile: {e}")
            return False
    
    def add_xp(self, amount: int) -> Tuple[int, List[Dict[str, Any]]]:
        """
        Add XP to the user's profile and check for level up.
        
        Args:
            amount: Amount of XP to add
            
        Returns:
            tuple: (new_level, new_achievements)
                new_level: The user's new level after adding XP
                new_achievements: List of new achievements earned
        """
        if amount <= 0:
            return self.level, []
            
        old_level = self.level
        self.xp += amount
        new_achievements = []
        
        # Check for level up
        while self.xp >= self.next_level_xp:
            self.level += 1
            self.xp -= self.next_level_xp
            
            # Increase XP required for next level (formula can be adjusted)
            self.next_level_xp = int(self.next_level_xp * 1.5)
            
            # Add level up achievement
            level_achievement = {
                'id': f'level_{self.level}',
                'title': f'Reached Level {self.level}',
                'description': f'You have reached level {self.level}!',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(level_achievement)
            new_achievements.append(level_achievement)
        
        # Save changes
        self.save()
        
        return self.level, new_achievements
    
    def complete_tutorial(self, tutorial_id: str, score: int = 100) -> Tuple[int, List[Dict[str, Any]]]:
        """
        Mark a tutorial as completed and award XP.
        
        Args:
            tutorial_id: ID of the completed tutorial
            score: Score achieved in the tutorial (0-100)
            
        Returns:
            tuple: (xp_earned, new_achievements)
                xp_earned: Amount of XP earned
                new_achievements: List of new achievements earned
        """
        # Check if already completed
        if tutorial_id in self.completed_tutorials:
            return 0, []
            
        # Calculate XP based on score
        xp_earned = int(100 * (score / 100))
        
        # Add to completed tutorials
        completion_info = {
            'id': tutorial_id,
            'completed_at': datetime.now().isoformat(),
            'score': score
        }
        self.completed_tutorials.append(completion_info)
        
        # Add XP
        _, new_achievements = self.add_xp(xp_earned)
        
        # Check for tutorial achievements
        if len(self.completed_tutorials) >= 5:
            self._check_tutorial_count_achievements(new_achievements)
        
        # Save changes
        self.save()
        
        return xp_earned, new_achievements
    
    def complete_challenge(self, challenge_id: str, score: int = 100) -> Tuple[int, List[Dict[str, Any]]]:
        """
        Mark a challenge as completed and award XP.
        
        Args:
            challenge_id: ID of the completed challenge
            score: Score achieved in the challenge (0-100)
            
        Returns:
            tuple: (xp_earned, new_achievements)
                xp_earned: Amount of XP earned
                new_achievements: List of new achievements earned
        """
        # Check if already completed
        if challenge_id in self.completed_challenges:
            return 0, []
            
        # Calculate XP based on score
        xp_earned = int(150 * (score / 100))  # Challenges give more XP than tutorials
        
        # Add to completed challenges
        completion_info = {
            'id': challenge_id,
            'completed_at': datetime.now().isoformat(),
            'score': score
        }
        self.completed_challenges.append(completion_info)
        
        # Add XP
        _, new_achievements = self.add_xp(xp_earned)
        
        # Check for challenge achievements
        if len(self.completed_challenges) >= 5:
            self._check_challenge_count_achievements(new_achievements)
        
        # Save changes
        self.save()
        
        return xp_earned, new_achievements
    
    def update_certification_progress(self, cert_id: str, domain: str, progress: float) -> None:
        """
        Update progress for a certification domain.
        
        Args:
            cert_id: Certification ID
            domain: Domain name
            progress: Progress value (0-100)
        """
        if cert_id not in self.certification_progress:
            self.certification_progress[cert_id] = {
                'domains': {},
                'total_progress': 0.0
            }
            
        # Update domain progress
        self.certification_progress[cert_id]['domains'][domain] = progress
        
        # Recalculate total progress
        domains = self.certification_progress[cert_id]['domains']
        if domains:
            total_progress = sum(domains.values()) / len(domains)
            self.certification_progress[cert_id]['total_progress'] = total_progress
            
            # Check for certification achievements
            if total_progress >= 25:
                self._add_certification_achievement(cert_id, 'beginner')
            if total_progress >= 50:
                self._add_certification_achievement(cert_id, 'intermediate')
            if total_progress >= 75:
                self._add_certification_achievement(cert_id, 'advanced')
            if total_progress >= 100:
                self._add_certification_achievement(cert_id, 'master')
        
        # Save changes
        self.save()
    
    def update_skill_level(self, skill: str, level: int) -> None:
        """
        Update the level for a specific skill.
        
        Args:
            skill: Skill name
            level: Skill level (1-10)
        """
        self.skill_levels[skill] = level
        self.save()
    
    def get_achievement_count(self) -> int:
        """
        Get the number of achievements earned.
        
        Returns:
            int: Number of achievements
        """
        return len(self.achievements)
    
    def get_tutorial_count(self) -> int:
        """
        Get the number of completed tutorials.
        
        Returns:
            int: Number of completed tutorials
        """
        return len(self.completed_tutorials)
    
    def get_challenge_count(self) -> int:
        """
        Get the number of completed challenges.
        
        Returns:
            int: Number of completed challenges
        """
        return len(self.completed_challenges)
    
    def get_certification_progress(self, cert_id: str) -> Dict[str, Any]:
        """
        Get progress for a specific certification.
        
        Args:
            cert_id: Certification ID
            
        Returns:
            dict: Certification progress data
        """
        return self.certification_progress.get(cert_id, {
            'domains': {},
            'total_progress': 0.0
        })
    
    def has_completed_tutorial(self, tutorial_id: str) -> bool:
        """
        Check if the user has completed a specific tutorial.
        
        Args:
            tutorial_id: Tutorial ID
            
        Returns:
            bool: True if completed, False otherwise
        """
        return any(t.get('id') == tutorial_id for t in self.completed_tutorials)
    
    def has_completed_challenge(self, challenge_id: str) -> bool:
        """
        Check if the user has completed a specific challenge.
        
        Args:
            challenge_id: Challenge ID
            
        Returns:
            bool: True if completed, False otherwise
        """
        return any(c.get('id') == challenge_id for c in self.completed_challenges)
    
    def has_achievement(self, achievement_id: str) -> bool:
        """
        Check if the user has a specific achievement.
        
        Args:
            achievement_id: Achievement ID
            
        Returns:
            bool: True if has achievement, False otherwise
        """
        return any(a.get('id') == achievement_id for a in self.achievements)
    
    def get_profile_summary(self) -> Dict[str, Any]:
        """
        Get a summary of the user's profile.
        
        Returns:
            dict: Profile summary
        """
        return {
            'username': self.username,
            'level': self.level,
            'xp': self.xp,
            'next_level_xp': self.next_level_xp,
            'tutorial_count': self.get_tutorial_count(),
            'challenge_count': self.get_challenge_count(),
            'achievement_count': self.get_achievement_count(),
            'certification_count': len(self.certification_progress)
        }
    
    def _check_tutorial_count_achievements(self, new_achievements: List[Dict[str, Any]]) -> None:
        """
        Check for tutorial count achievements.
        
        Args:
            new_achievements: List to add new achievements to
        """
        count = self.get_tutorial_count()
        
        if count >= 5 and not self.has_achievement('tutorials_5'):
            achievement = {
                'id': 'tutorials_5',
                'title': 'Tutorial Apprentice',
                'description': 'Complete 5 tutorials',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            new_achievements.append(achievement)
            
        if count >= 10 and not self.has_achievement('tutorials_10'):
            achievement = {
                'id': 'tutorials_10',
                'title': 'Tutorial Adept',
                'description': 'Complete 10 tutorials',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            new_achievements.append(achievement)
            
        if count >= 25 and not self.has_achievement('tutorials_25'):
            achievement = {
                'id': 'tutorials_25',
                'title': 'Tutorial Master',
                'description': 'Complete 25 tutorials',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            new_achievements.append(achievement)
    
    def _check_challenge_count_achievements(self, new_achievements: List[Dict[str, Any]]) -> None:
        """
        Check for challenge count achievements.
        
        Args:
            new_achievements: List to add new achievements to
        """
        count = self.get_challenge_count()
        
        if count >= 5 and not self.has_achievement('challenges_5'):
            achievement = {
                'id': 'challenges_5',
                'title': 'Challenge Seeker',
                'description': 'Complete 5 challenges',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            new_achievements.append(achievement)
            
        if count >= 10 and not self.has_achievement('challenges_10'):
            achievement = {
                'id': 'challenges_10',
                'title': 'Challenge Conqueror',
                'description': 'Complete 10 challenges',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            new_achievements.append(achievement)
            
        if count >= 25 and not self.has_achievement('challenges_25'):
            achievement = {
                'id': 'challenges_25',
                'title': 'Challenge Champion',
                'description': 'Complete 25 challenges',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            new_achievements.append(achievement)
    
    def _add_certification_achievement(self, cert_id: str, level: str) -> None:
        """
        Add a certification progress achievement.
        
        Args:
            cert_id: Certification ID
            level: Achievement level (beginner, intermediate, advanced, master)
        """
        achievement_id = f'cert_{cert_id}_{level}'
        
        if not self.has_achievement(achievement_id):
            achievement = {
                'id': achievement_id,
                'title': f'{cert_id.upper()} {level.title()} Level',
                'description': f'Reach {level.title()} level in {cert_id.upper()} certification',
                'earned_at': datetime.now().isoformat()
            }
            self.achievements.append(achievement)
            self.save()
