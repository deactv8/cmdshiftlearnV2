"""
XP and leveling system for CmdShiftLearn.
"""

from typing import Dict, List, Any, Tuple
from math import floor


class XPSystem:
    """XP and leveling system for gamification."""
    
    def __init__(self):
        """Initialize the XP system."""
        # Level thresholds (total XP needed to reach each level)
        self.level_thresholds = self._generate_level_thresholds()
        
        # XP rewards for different activities
        self.xp_rewards = {
            'tutorial_completion': {
                'beginner': 100,
                'intermediate': 200,
                'advanced': 300
            },
            'challenge_completion': {
                'beginner': 150,
                'intermediate': 300,
                'advanced': 500
            },
            'certification_progress': {
                'milestone_25': 250,
                'milestone_50': 500,
                'milestone_75': 750,
                'milestone_100': 1000
            },
            'skill_milestone': {
                'level_3': 100,
                'level_5': 200,
                'level_7': 300,
                'level_10': 500
            },
            'daily_login': 10,
            'streak': {
                'week': 50,
                'month': 200
            }
        }
    
    def _generate_level_thresholds(self, max_level: int = 50) -> List[int]:
        """
        Generate XP thresholds for each level.
        
        Args:
            max_level: Maximum level to generate thresholds for
            
        Returns:
            list: List of XP thresholds for each level
        """
        thresholds = [0]  # Level 0 is 0 XP
        
        # Simple formula: each level requires 50% more XP than the previous level
        base_xp = 100
        for level in range(1, max_level + 1):
            # Level 1: 100 XP
            # Level 2: 250 XP (100 + 150)
            # Level 3: 475 XP (250 + 225)
            # etc.
            if level == 1:
                next_threshold = base_xp
            else:
                xp_for_level = thresholds[level - 1] * 0.5
                next_threshold = thresholds[level - 1] + xp_for_level
            
            thresholds.append(int(next_threshold))
        
        return thresholds
    
    def calculate_level(self, total_xp: int) -> Tuple[int, int, int]:
        """
        Calculate level based on total XP.
        
        Args:
            total_xp: Total XP earned
            
        Returns:
            tuple: (level, current_level_xp, next_level_xp)
                level: Current level
                current_level_xp: XP in the current level
                next_level_xp: XP needed for next level
        """
        # Find the highest level where the threshold is <= total_xp
        level = 0
        for i, threshold in enumerate(self.level_thresholds):
            if total_xp >= threshold:
                level = i
            else:
                break
        
        # Calculate XP in the current level
        current_level_threshold = self.level_thresholds[level]
        current_level_xp = total_xp - current_level_threshold
        
        # Calculate XP needed for next level
        next_level = min(level + 1, len(self.level_thresholds) - 1)
        next_level_threshold = self.level_thresholds[next_level]
        next_level_xp = next_level_threshold - current_level_threshold
        
        return level, current_level_xp, next_level_xp
    
    def get_xp_reward(self, activity: str, difficulty: str = None, milestone: str = None) -> int:
        """
        Get XP reward for an activity.
        
        Args:
            activity: Activity type (tutorial_completion, challenge_completion, etc.)
            difficulty: Difficulty level for the activity (beginner, intermediate, advanced)
            milestone: Milestone for activities with milestones
            
        Returns:
            int: XP reward
        """
        if activity not in self.xp_rewards:
            return 0
            
        rewards = self.xp_rewards[activity]
        
        if isinstance(rewards, dict):
            if difficulty and difficulty in rewards:
                return rewards[difficulty]
            elif milestone and milestone in rewards:
                return rewards[milestone]
            else:
                # Default to the first reward
                return next(iter(rewards.values())) if rewards else 0
        else:
            return rewards
    
    def calculate_xp_with_multipliers(self, base_xp: int, multipliers: Dict[str, float] = None) -> int:
        """
        Calculate XP with multipliers.
        
        Args:
            base_xp: Base XP reward
            multipliers: Dictionary of multipliers and their values
            
        Returns:
            int: Final XP reward
        """
        if not multipliers:
            return base_xp
            
        final_xp = base_xp
        for multiplier, value in multipliers.items():
            final_xp *= value
            
        return int(final_xp)
    
    def get_level_title(self, level: int) -> str:
        """
        Get a title for a level.
        
        Args:
            level: Level
            
        Returns:
            str: Level title
        """
        if level < 5:
            return "PowerShell Novice"
        elif level < 10:
            return "PowerShell Apprentice"
        elif level < 15:
            return "PowerShell Adept"
        elif level < 20:
            return "PowerShell Expert"
        elif level < 30:
            return "PowerShell Master"
        else:
            return "PowerShell Grandmaster"
    
    def get_level_progress_percentage(self, current_xp: int, next_level_xp: int) -> float:
        """
        Calculate progress percentage towards next level.
        
        Args:
            current_xp: XP in the current level
            next_level_xp: XP needed for next level
            
        Returns:
            float: Progress percentage (0-100)
        """
        if next_level_xp <= 0:
            return 100.0
            
        progress = (current_xp / next_level_xp) * 100
        return min(100.0, progress)
