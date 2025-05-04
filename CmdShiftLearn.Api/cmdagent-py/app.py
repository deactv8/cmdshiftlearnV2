"""
Main application entry point for CmdShiftLearn.
"""

import os
import sys
from pathlib import Path
from typing import Dict, List, Any, Optional, Tuple

from terminal.ui import TerminalUI
from terminal.input_handler import InputHandler
from powershell.executor import PowerShellExecutor
from powershell.validator import PowerShellValidator
from content.manager import ContentManager
from content.repository import ContentRepository
from user.profile import UserProfile
from user.progress import ProgressTracker
from gamification.xp import XPSystem
from gamification.achievements import AchievementSystem
from certification.tracker import CertificationTracker

from utils.config import load_config, DATA_DIR


class CmdShiftLearn:
    """Main application class for CmdShiftLearn."""
    
    def __init__(self):
        """Initialize the application."""
        # Load configuration
        self.config = load_config()
        
        # Initialize components
        self.user_profile = None
        self.terminal_ui = TerminalUI(self)
        self.input_handler = InputHandler()
        self.powershell_executor = PowerShellExecutor(sandbox_mode=True)
        self.powershell_validator = PowerShellValidator()
        
        self.content_repository = ContentRepository()
        self.content_manager = ContentManager(self.content_repository)
        
        self.xp_system = XPSystem()
        self.achievement_system = AchievementSystem()
        self.certification_tracker = CertificationTracker()
        
        self.progress_tracker = None  # Will be initialized after user profile is loaded
    
    def start(self):
        """Start the CmdShiftLearn application."""
        # Show welcome screen
        self.terminal_ui.show_welcome_screen()
        
        # Load or create user profile
        self.load_or_create_profile()
        
        # Initialize progress tracker with user profile
        self.progress_tracker = ProgressTracker(self.user_profile)
        
        # Start main loop
        self.terminal_ui.start_main_loop()
    
    def load_or_create_profile(self):
        """Load existing user profile or create a new one."""
        # Ask for username
        username = self.terminal_ui.prompt_for_username()
        
        # Try to load profile
        self.user_profile = UserProfile.load(username)
        
        # Create new profile if it doesn't exist
        if not self.user_profile:
            self.user_profile = UserProfile.create(username)
    
    def start_tutorial(self):
        """Start a tutorial."""
        # Get available tutorials
        tutorials = self.content_manager.get_tutorial_list()
        
        # Display tutorials and get selection
        selected_tutorial = self.terminal_ui.display_tutorials(tutorials)
        
        if not selected_tutorial:
            return
        
        # Display tutorial details and ask if user wants to start
        start_tutorial = self.terminal_ui.display_tutorial(selected_tutorial)
        
        if not start_tutorial:
            return
        
        # Run the tutorial
        self._run_tutorial(selected_tutorial)
    
    def start_challenge(self):
        """Start a challenge."""
        # Get available challenges
        challenges = self.content_manager.get_challenge_list()
        
        # Display challenges and get selection (reusing tutorial UI for now)
        selected_challenge = self.terminal_ui.display_tutorials(challenges)
        
        if not selected_challenge:
            return
        
        # Display challenge details and ask if user wants to start
        start_challenge = self.terminal_ui.display_tutorial(selected_challenge)
        
        if not start_challenge:
            return
        
        # Run the challenge
        self._run_challenge(selected_challenge)
    
    def view_certification_progress(self):
        """View certification progress."""
        # Get certification progress
        cert_progress = self.user_profile.certification_progress
        
        # Get certification data
        certifications = []
        for cert_id, progress in cert_progress.items():
            cert = self.certification_tracker.get_certification(cert_id)
            if cert:
                cert_data = {
                    'id': cert_id,
                    'title': cert.get('title', f'Certification {cert_id.upper()}'),
                    'progress': progress.get('total_progress', 0),
                    'domains': []
                }
                
                # Add domain progress
                for domain_id, domain_progress in progress.get('domains', {}).items():
                    domain = self.certification_tracker.get_certification_domain(cert_id, domain_id)
                    if domain:
                        cert_data['domains'].append({
                            'name': domain.get('name', domain_id),
                            'progress': domain_progress
                        })
                
                certifications.append(cert_data)
        
        # Display certification progress
        self.terminal_ui.display_certification_progress(certifications)
    
    def start_powershell_playground(self):
        """Start the PowerShell playground."""
        # Clear screen
        self.terminal_ui.clear_screen()
        
        # Display playground header
        self.terminal_ui.console.print("\n[bold cyan]PowerShell Playground[/bold cyan]")
        self.terminal_ui.console.print("[italic]Practice PowerShell commands in a safe environment.[/italic]")
        self.terminal_ui.console.print()
        
        # Start playground loop
        while True:
            # Get command input
            command = self.terminal_ui.get_command_input(None)
            
            # Exit playground if command is 'exit'
            if command.lower() == 'exit':
                break
            
            # Execute command
            success, stdout, stderr = self.powershell_executor.execute_command(command)
            
            # Display result
            self.terminal_ui.display_command_result(success, stdout, stderr)
    
    def view_profile(self):
        """View user profile."""
        # Get profile data
        profile_data = {
            'username': self.user_profile.username,
            'level': self.user_profile.level,
            'xp': self.user_profile.xp,
            'next_level_xp': self.user_profile.next_level_xp,
            'achievements': self.user_profile.achievements,
            'completed_tutorials': self.user_profile.completed_tutorials,
            'completed_challenges': self.user_profile.completed_challenges,
        }
        
        # Display profile
        self.terminal_ui.display_profile(profile_data)
    
    def show_settings(self):
        """Show and edit user settings."""
        # For now, just show a message
        self.terminal_ui.console.print("\n[bold cyan]Settings[/bold cyan]")
        self.terminal_ui.console.print("Settings feature is not yet implemented.")
        self.terminal_ui.console.print()
        
        # Wait for user acknowledgment
        self.terminal_ui.console.input("Press Enter to continue...")
    
    def get_tutorial_status(self, tutorial_id: str) -> str:
        """
        Get the status of a tutorial for the current user.
        
        Args:
            tutorial_id: Tutorial ID
            
        Returns:
            str: Status string (Not Started, In Progress, Completed)
        """
        # Check if tutorial is completed
        if self.user_profile.has_completed_tutorial(tutorial_id):
            return "Completed"
        
        # Check if tutorial is in progress
        progress = self.progress_tracker.get_tutorial_progress(tutorial_id)
        if progress and progress.get('last_step', 0) > 0:
            return "In Progress"
        
        # Default to Not Started
        return "Not Started"
    
    def _run_tutorial(self, tutorial: Dict[str, Any]):
        """
        Run a tutorial.
        
        Args:
            tutorial: Tutorial data
        """
        tutorial_id = tutorial.get('id')
        steps = tutorial.get('steps', [])
        
        if not steps:
            self.terminal_ui.console.print("[yellow]This tutorial has no interactive steps.[/yellow]")
            return
        
        # Loop through steps
        for i, step in enumerate(steps):
            # Track progress
            self.progress_tracker.track_tutorial_progress(tutorial_id, i)
            
            # Display step
            self.terminal_ui.display_tutorial_step(step, i + 1, len(steps))
            
            # If this is a command step, prompt for input
            step_type = step.get('type', '')
            if step_type == 'command' or step_type == 'challenge':
                # Get expected command
                expected_command = step.get('command', '') or step.get('expected_command', '')
                
                # Get user input
                user_input = self.terminal_ui.get_command_input(step)
                
                # Validate input
                is_correct, feedback = self.input_handler.check_command(
                    user_input, 
                    expected_command,
                    step.get('validation_type', 'exact')
                )
                
                # Provide a hint if incorrect
                if not is_correct:
                    hint = step.get('hint', "Try reviewing the instructions.")
                    self.terminal_ui.display_feedback(is_correct, feedback, hint)
                    
                    # Give a second attempt
                    user_input = self.terminal_ui.get_command_input(step)
                    is_correct, feedback = self.input_handler.check_command(
                        user_input, 
                        expected_command,
                        step.get('validation_type', 'exact')
                    )
                
                # Display final feedback
                self.terminal_ui.display_feedback(is_correct, feedback)
                
                # If correct, execute the command to show the result
                if is_correct:
                    success, stdout, stderr = self.powershell_executor.execute_command(user_input)
                    self.terminal_ui.display_command_result(success, stdout, stderr)
            
            # Wait for user to continue
            self.terminal_ui.console.input("\nPress Enter to continue...")
        
        # Complete the tutorial
        self._complete_tutorial(tutorial)
    
    def _complete_tutorial(self, tutorial: Dict[str, Any]):
        """
        Complete a tutorial and award XP.
        
        Args:
            tutorial: Tutorial data
        """
        tutorial_id = tutorial.get('id')
        xp_reward = tutorial.get('xp_reward', 100)
        
        # Mark tutorial as completed with 100% score
        progress_data = self.progress_tracker.track_tutorial_progress(
            tutorial_id, 
            len(tutorial.get('steps', [])) - 1,
            True,
            100
        )
        
        # Get new achievements
        new_achievements = progress_data.get('new_achievements', [])
        
        # Show completion message
        self.terminal_ui.console.print("\n[bold green]Tutorial Completed![/bold green]")
        self.terminal_ui.console.print(f"You earned [bold magenta]{xp_reward} XP[/bold magenta]")
        
        # Show new level if leveled up
        old_level = self.user_profile.level - 1 if progress_data.get('leveled_up') else self.user_profile.level
        if old_level < self.user_profile.level:
            self.terminal_ui.console.print(f"[bold cyan]Level Up![/bold cyan] You are now level [bold]{self.user_profile.level}[/bold]")
        
        # Show new achievements
        for achievement in new_achievements:
            self.terminal_ui.display_achievement(achievement)
        
        # Update certification progress if applicable
        cert_mappings = tutorial.get('certification_mappings', [])
        for mapping in cert_mappings:
            cert_id = mapping.get('cert_id')
            domain = mapping.get('domain')
            
            if cert_id and domain:
                # Get current progress
                cert_progress = self.user_profile.get_certification_progress(cert_id)
                domain_progress = cert_progress.get('domains', {}).get(domain, 0)
                
                # Update progress (increase by 5% for each completed tutorial)
                new_progress = min(100, domain_progress + 5)
                self.user_profile.update_certification_progress(cert_id, domain, new_progress)
                
                self.terminal_ui.console.print(f"Progress in [bold]{cert_id.upper()}[/bold] certification domain [bold]{domain}[/bold] increased to [bold yellow]{new_progress:.1f}%[/bold yellow]")
        
        # Wait for user acknowledgment
        self.terminal_ui.console.input("\nPress Enter to continue...")
    
    def _run_challenge(self, challenge: Dict[str, Any]):
        """
        Run a challenge.
        
        Args:
            challenge: Challenge data
        """
        challenge_id = challenge.get('id')
        
        # Display challenge
        self.terminal_ui.display_challenge(challenge)
        
        # Get expected solution
        expected_solution = challenge.get('solution', '')
        
        # Get user input
        user_input = self.terminal_ui.get_command_input(challenge)
        
        # Validate input (more flexible validation for challenges)
        is_correct, feedback = self.input_handler.check_command(
            user_input, 
            expected_solution,
            challenge.get('validation_type', 'fuzzy')
        )
        
        # Provide a hint if incorrect
        if not is_correct:
            hint = challenge.get('hint', "Try a different approach.")
            self.terminal_ui.display_feedback(is_correct, feedback, hint)
            
            # Give a second attempt
            user_input = self.terminal_ui.get_command_input(challenge)
            is_correct, feedback = self.input_handler.check_command(
                user_input, 
                expected_solution,
                challenge.get('validation_type', 'fuzzy')
            )
        
        # Display final feedback
        self.terminal_ui.display_feedback(is_correct, feedback)
        
        # If correct, execute the command to show the result
        if is_correct:
            success, stdout, stderr = self.powershell_executor.execute_command(user_input)
            self.terminal_ui.display_command_result(success, stdout, stderr)
            
            # Complete the challenge
            score = 100 if is_correct else 50
            self._complete_challenge(challenge, score)
        else:
            # Show the solution
            self.terminal_ui.console.print("\n[bold yellow]Solution:[/bold yellow]")
            self.terminal_ui.console.print(f"{expected_solution}")
            
            # Complete with partial score
            self._complete_challenge(challenge, 25)
        
        # Wait for user acknowledgment
        self.terminal_ui.console.input("\nPress Enter to continue...")
    
    def _complete_challenge(self, challenge: Dict[str, Any], score: int):
        """
        Complete a challenge and award XP.
        
        Args:
            challenge: Challenge data
            score: Score achieved (0-100)
        """
        challenge_id = challenge.get('id')
        xp_reward = challenge.get('xp_reward', 150)
        
        # Adjust XP based on score
        xp_awarded = int(xp_reward * (score / 100))
        
        # Mark challenge as completed
        progress_data = self.progress_tracker.track_challenge_progress(
            challenge_id, 
            True,
            score
        )
        
        # Get new achievements
        new_achievements = progress_data.get('new_achievements', [])
        
        # Show completion message
        self.terminal_ui.console.print("\n[bold green]Challenge Completed![/bold green]")
        self.terminal_ui.console.print(f"Score: [bold yellow]{score}%[/bold yellow]")
        self.terminal_ui.console.print(f"You earned [bold magenta]{xp_awarded} XP[/bold magenta]")
        
        # Show new level if leveled up
        old_level = self.user_profile.level - 1 if progress_data.get('leveled_up') else self.user_profile.level
        if old_level < self.user_profile.level:
            self.terminal_ui.console.print(f"[bold cyan]Level Up![/bold cyan] You are now level [bold]{self.user_profile.level}[/bold]")
        
        # Show new achievements
        for achievement in new_achievements:
            self.terminal_ui.display_achievement(achievement)


if __name__ == "__main__":
    app = CmdShiftLearn()
    app.start()
