"""
Achievement system for CmdShiftLearn.
"""

from typing import Dict, List, Any, Optional
from datetime import datetime


class AchievementSystem:
    """System for tracking and awarding achievements."""
    
    def __init__(self):
        """Initialize the achievement system."""
        # Define achievement categories
        self.categories = {
            'tutorial': 'Tutorial Achievements',
            'challenge': 'Challenge Achievements',
            'certification': 'Certification Achievements',
            'skill': 'Skill Achievements',
            'streak': 'Streak Achievements',
            'special': 'Special Achievements'
        }
        
        # Define achievements
        self.achievements = self._load_achievements()
    
    def _load_achievements(self) -> Dict[str, Dict[str, Any]]:
        """
        Load achievement definitions.
        
        Returns:
            dict: Dictionary of achievement definitions
        """
        # Dictionary to store achievements
        achievements = {}
        
        # Tutorial achievements
        achievements['tutorial_1'] = {
            'id': 'tutorial_1',
            'title': 'First Steps',
            'description': 'Complete your first tutorial',
            'category': 'tutorial',
            'icon': '🎓',
            'xp_reward': 50
        }
        
        achievements['tutorial_5'] = {
            'id': 'tutorial_5',
            'title': 'Tutorial Apprentice',
            'description': 'Complete 5 tutorials',
            'category': 'tutorial',
            'icon': '📚',
            'xp_reward': 100
        }
        
        achievements['tutorial_10'] = {
            'id': 'tutorial_10',
            'title': 'Tutorial Adept',
            'description': 'Complete 10 tutorials',
            'category': 'tutorial',
            'icon': '📚',
            'xp_reward': 200
        }
        
        achievements['tutorial_25'] = {
            'id': 'tutorial_25',
            'title': 'Tutorial Master',
            'description': 'Complete 25 tutorials',
            'category': 'tutorial',
            'icon': '👨‍🏫',
            'xp_reward': 500
        }
        
        # Challenge achievements
        achievements['challenge_1'] = {
            'id': 'challenge_1',
            'title': 'Challenge Accepted',
            'description': 'Complete your first challenge',
            'category': 'challenge',
            'icon': '🏆',
            'xp_reward': 75
        }
        
        achievements['challenge_5'] = {
            'id': 'challenge_5',
            'title': 'Challenge Seeker',
            'description': 'Complete 5 challenges',
            'category': 'challenge',
            'icon': '🏆',
            'xp_reward': 150
        }
        
        achievements['challenge_10'] = {
            'id': 'challenge_10',
            'title': 'Challenge Conqueror',
            'description': 'Complete 10 challenges',
            'category': 'challenge',
            'icon': '🏆',
            'xp_reward': 300
        }
        
        achievements['challenge_25'] = {
            'id': 'challenge_25',
            'title': 'Challenge Champion',
            'description': 'Complete 25 challenges',
            'category': 'challenge',
            'icon': '👑',
            'xp_reward': 750
        }
        
        # Certification achievements
        achievements['cert_az104_beginner'] = {
            'id': 'cert_az104_beginner',
            'title': 'AZ-104 Beginner',
            'description': 'Reach 25% progress in AZ-104 certification',
            'category': 'certification',
            'icon': '🌱',
            'xp_reward': 250
        }
        
        achievements['cert_az104_intermediate'] = {
            'id': 'cert_az104_intermediate',
            'title': 'AZ-104 Intermediate',
            'description': 'Reach 50% progress in AZ-104 certification',
            'category': 'certification',
            'icon': '🌿',
            'xp_reward': 500
        }
        
        achievements['cert_az104_advanced'] = {
            'id': 'cert_az104_advanced',
            'title': 'AZ-104 Advanced',
            'description': 'Reach 75% progress in AZ-104 certification',
            'category': 'certification',
            'icon': '🌳',
            'xp_reward': 750
        }
        
        achievements['cert_az104_master'] = {
            'id': 'cert_az104_master',
            'title': 'AZ-104 Master',
            'description': 'Complete AZ-104 certification preparation',
            'category': 'certification',
            'icon': '🏅',
            'xp_reward': 1000
        }
        
        # SC-300 Certification achievements
        achievements['cert_sc300_beginner'] = {
            'id': 'cert_sc300_beginner',
            'title': 'SC-300 Beginner',
            'description': 'Reach 25% progress in SC-300 certification',
            'category': 'certification',
            'icon': '🌱',
            'xp_reward': 250
        }
        
        achievements['cert_sc300_intermediate'] = {
            'id': 'cert_sc300_intermediate',
            'title': 'SC-300 Intermediate',
            'description': 'Reach 50% progress in SC-300 certification',
            'category': 'certification',
            'icon': '🌿',
            'xp_reward': 500
        }
        
        achievements['cert_sc300_advanced'] = {
            'id': 'cert_sc300_advanced',
            'title': 'SC-300 Advanced',
            'description': 'Reach 75% progress in SC-300 certification',
            'category': 'certification',
            'icon': '🌳',
            'xp_reward': 750
        }
        
        achievements['cert_sc300_master'] = {
            'id': 'cert_sc300_master',
            'title': 'SC-300 Master',
            'description': 'Complete SC-300 certification preparation',
            'category': 'certification',
            'icon': '🏅',
            'xp_reward': 1000
        }
        
        # Skill achievements
        achievements['skill_powershell_basics'] = {
            'id': 'skill_powershell_basics',
            'title': 'PowerShell Fundamentals',
            'description': 'Master the basics of PowerShell',
            'category': 'skill',
            'icon': '💻',
            'xp_reward': 100
        }
        
        achievements['skill_azure_basics'] = {
            'id': 'skill_azure_basics',
            'title': 'Azure Fundamentals',
            'description': 'Learn the basics of Azure management with PowerShell',
            'category': 'skill',
            'icon': '☁️',
            'xp_reward': 150
        }
        
        achievements['skill_security_basics'] = {
            'id': 'skill_security_basics',
            'title': 'Security Fundamentals',
            'description': 'Learn the basics of security management with PowerShell',
            'category': 'skill',
            'icon': '🔒',
            'xp_reward': 150
        }
        
        # Streak achievements
        achievements['streak_week'] = {
            'id': 'streak_week',
            'title': 'Weekly Warrior',
            'description': 'Log in for 7 consecutive days',
            'category': 'streak',
            'icon': '📅',
            'xp_reward': 50
        }
        
        achievements['streak_month'] = {
            'id': 'streak_month',
            'title': 'Monthly Master',
            'description': 'Log in for 30 consecutive days',
            'category': 'streak',
            'icon': '🗓️',
            'xp_reward': 200
        }
        
        # Special achievements
        achievements['perfect_score'] = {
            'id': 'perfect_score',
            'title': 'Perfect Score',
            'description': 'Complete a tutorial or challenge with a perfect score',
            'category': 'special',
            'icon': '🌟',
            'xp_reward': 100
        }
        
        achievements['night_owl'] = {
            'id': 'night_owl',
            'title': 'Night Owl',
            'description': 'Complete a tutorial between midnight and 5 AM',
            'category': 'special',
            'icon': '🦉',
            'xp_reward': 50
        }
        
        achievements['weekend_warrior'] = {
            'id': 'weekend_warrior',
            'title': 'Weekend Warrior',
            'description': 'Complete 3 tutorials or challenges on a weekend',
            'category': 'special',
            'icon': '🎮',
            'xp_reward': 75
        }
        
        # Level achievements
        for level in [5, 10, 25, 50]:
            achievements[f'level_{level}'] = {
                'id': f'level_{level}',
                'title': f'Level {level} Achieved',
                'description': f'Reach level {level}',
                'category': 'special',
                'icon': '⭐',
                'xp_reward': level * 10
            }
        
        return achievements
    
    def get_achievement(self, achievement_id: str) -> Optional[Dict[str, Any]]:
        """
        Get an achievement by ID.
        
        Args:
            achievement_id: Achievement ID
            
        Returns:
            dict: Achievement data or None if not found
        """
        return self.achievements.get(achievement_id)
    
    def get_achievements_by_category(self, category: str) -> List[Dict[str, Any]]:
        """
        Get achievements by category.
        
        Args:
            category: Achievement category
            
        Returns:
            list: List of achievements in the category
        """
        return [a for a in self.achievements.values() if a.get('category') == category]
    
    def get_all_achievements(self) -> List[Dict[str, Any]]:
        """
        Get all achievements.
        
        Returns:
            list: List of all achievements
        """
        return list(self.achievements.values())
    
    def check_achievements(self, user_data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """
        Check which achievements a user has earned.
        
        Args:
            user_data: User data including profile and progress
            
        Returns:
            list: List of earned achievements
        """
        # Extract relevant data from user data
        earned_achievements = []
        
        # Tutorial achievements
        tutorial_count = len(user_data.get('completed_tutorials', []))
        
        if tutorial_count >= 1:
            earned_achievements.append(self.achievements['tutorial_1'])
        if tutorial_count >= 5:
            earned_achievements.append(self.achievements['tutorial_5'])
        if tutorial_count >= 10:
            earned_achievements.append(self.achievements['tutorial_10'])
        if tutorial_count >= 25:
            earned_achievements.append(self.achievements['tutorial_25'])
        
        # Challenge achievements
        challenge_count = len(user_data.get('completed_challenges', []))
        
        if challenge_count >= 1:
            earned_achievements.append(self.achievements['challenge_1'])
        if challenge_count >= 5:
            earned_achievements.append(self.achievements['challenge_5'])
        if challenge_count >= 10:
            earned_achievements.append(self.achievements['challenge_10'])
        if challenge_count >= 25:
            earned_achievements.append(self.achievements['challenge_25'])
        
        # Certification achievements
        cert_progress = user_data.get('certification_progress', {})
        
        for cert_id, progress in cert_progress.items():
            total_progress = progress.get('total_progress', 0)
            
            if total_progress >= 25:
                achievement_id = f'cert_{cert_id}_beginner'
                if achievement_id in self.achievements:
                    earned_achievements.append(self.achievements[achievement_id])
                    
            if total_progress >= 50:
                achievement_id = f'cert_{cert_id}_intermediate'
                if achievement_id in self.achievements:
                    earned_achievements.append(self.achievements[achievement_id])
                    
            if total_progress >= 75:
                achievement_id = f'cert_{cert_id}_advanced'
                if achievement_id in self.achievements:
                    earned_achievements.append(self.achievements[achievement_id])
                    
            if total_progress >= 100:
                achievement_id = f'cert_{cert_id}_master'
                if achievement_id in self.achievements:
                    earned_achievements.append(self.achievements[achievement_id])
        
        # Level achievements
        level = user_data.get('level', 1)
        
        if level >= 5:
            earned_achievements.append(self.achievements['level_5'])
        if level >= 10:
            earned_achievements.append(self.achievements['level_10'])
        if level >= 25:
            earned_achievements.append(self.achievements['level_25'])
        if level >= 50:
            earned_achievements.append(self.achievements['level_50'])
        
        # Perfect score achievement
        for tutorial in user_data.get('completed_tutorials', []):
            if tutorial.get('score', 0) == 100:
                earned_achievements.append(self.achievements['perfect_score'])
                break
                
        if 'perfect_score' not in [a.get('id') for a in earned_achievements]:
            for challenge in user_data.get('completed_challenges', []):
                if challenge.get('score', 0) == 100:
                    earned_achievements.append(self.achievements['perfect_score'])
                    break
        
        return earned_achievements
    
    def get_unearned_achievements(self, user_data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """
        Get achievements that a user has not yet earned.
        
        Args:
            user_data: User data including profile and progress
            
        Returns:
            list: List of unearned achievements
        """
        earned_ids = [a.get('id') for a in self.check_achievements(user_data)]
        return [a for a in self.achievements.values() if a.get('id') not in earned_ids]
    
    def check_for_new_achievements(self, user_data: Dict[str, Any], 
                                  previous_achievements: List[str]) -> List[Dict[str, Any]]:
        """
        Check for newly earned achievements.
        
        Args:
            user_data: User data including profile and progress
            previous_achievements: List of previously earned achievement IDs
            
        Returns:
            list: List of newly earned achievements
        """
        # Get current achievements
        current_achievements = self.check_achievements(user_data)
        current_ids = [a.get('id') for a in current_achievements]
        
        # Find new achievements
        new_achievement_ids = [aid for aid in current_ids if aid not in previous_achievements]
        
        # Return new achievement objects
        return [a for a in current_achievements if a.get('id') in new_achievement_ids]
    
    def award_achievement(self, user_data: Dict[str, Any], achievement_id: str) -> Dict[str, Any]:
        """
        Award an achievement to a user.
        
        Args:
            user_data: User data to update
            achievement_id: Achievement ID to award
            
        Returns:
            dict: The awarded achievement or None if not found
        """
        achievement = self.get_achievement(achievement_id)
        if not achievement:
            return {}
            
        # Add achievement to user's achievements
        if 'achievements' not in user_data:
            user_data['achievements'] = []
            
        # Add achievement if not already earned
        achievement_ids = [a.get('id') for a in user_data['achievements']]
        if achievement_id not in achievement_ids:
            # Add earned_at timestamp
            achievement_with_timestamp = achievement.copy()
            achievement_with_timestamp['earned_at'] = datetime.now().isoformat()
            
            user_data['achievements'].append(achievement_with_timestamp)
            
            # Award XP if applicable
            xp_reward = achievement.get('xp_reward', 0)
            if xp_reward > 0 and 'xp' in user_data:
                user_data['xp'] += xp_reward
        
        return achievement
