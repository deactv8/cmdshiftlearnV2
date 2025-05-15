#!/usr/bin/env python3
"""
CmdShiftLearn CLI Agent - Enhanced Python Edition

A command-line tool for interacting with the CmdShiftLearn platform
with rich animated terminal experience.
"""

import sys
import os
import time
import logging
import random
from typing import List, Dict, Any, Optional, Tuple

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('main')

# Import the enhanced animated UI
from terminal.animated_ui import AnimatedTerminalUI
from api.tutorials import TutorialClient
from api.auth import login, load_api_key
import powershell.executor as ps_executor
from utils.config import API_BASE_URL

def check_command(user_input: str, expected_command: str, validation_type: str = 'exact', 
                 case_sensitive: bool = False, output_check: str = None) -> Tuple[bool, str]:
    """
    Check if the user's input matches the expected command.
    
    Args:
        user_input: The user's input command
        expected_command: The expected command
        validation_type: Type of validation ('exact', 'contains', 'output', 'regex')
        case_sensitive: Whether to perform case-sensitive matching
        output_check: Optional pattern to match against command output
        
    Returns:
        Tuple[bool, str]: (is_correct, feedback_message)
    """
    if validation_type == 'any':
        # Any input is valid
        return True, "Great! Let's continue."
    
    elif validation_type == 'exact':
        # Exact match (with case sensitivity option)
        if case_sensitive:
            is_correct = user_input == expected_command
        else:
            is_correct = user_input.lower() == expected_command.lower()
        
        if is_correct:
            return True, "Correct! Great job!"
        else:
            return False, "That's not quite right. Try again!"
    
    elif validation_type == 'contains':
        # Check if input contains expected command
        if case_sensitive:
            is_correct = expected_command in user_input
        else:
            is_correct = expected_command.lower() in user_input.lower()
        
        if is_correct:
            return True, "Good! Your command contains the expected pattern."
        else:
            return False, "Your command doesn't include the expected pattern."
    
    elif validation_type == 'regex':
        # Use regex pattern matching
        import re
        try:
            # If expected_command starts with regex:, extract the pattern
            if isinstance(expected_command, str) and expected_command.startswith('regex:'):
                pattern = expected_command[6:]
            else:
                pattern = expected_command
                
            # Attempt to match with the regex pattern
            flags = 0 if case_sensitive else re.IGNORECASE
            match = re.match(pattern, user_input, flags)
            is_correct = bool(match)
            
            if is_correct:
                return True, "Great! Your command matches the expected pattern."
            else:
                return False, "Your command doesn't match the expected pattern."
        except Exception as e:
            logger.error(f"Error in regex validation: {str(e)}")
            return False, "There was an error validating your command. Try again."
    
    elif validation_type == 'output' and output_check:
        # Execute command and check output
        success_result, stdout, stderr = ps_executor.execute_powershell_command(user_input)
        is_correct = output_check.lower() in stdout.lower() if success_result else False
        
        if is_correct:
            return True, "Great! Your command produced the expected output."
        else:
            return False, "Your command didn't produce the expected output."
    
    else:
        # Fall back to basic validation
        is_correct = user_input.lower() == expected_command.lower()
        
        if is_correct:
            return True, "Correct!"
        else:
            return False, "That's not quite right. Try again!"


def check_tutorial_validity(ui: AnimatedTerminalUI, tutorial: Dict[str, Any]) -> bool:
    """
    Check if a tutorial is valid and report its source.
    
    Args:
        ui: The animated terminal UI
        tutorial: The tutorial data
        
    Returns:
        bool: True if the tutorial is valid, False otherwise
    """
    if not tutorial:
        ui.display_error("Tutorial not found or could not be loaded.")
        return False
    
    # Ensure we have the steps key and it's a list
    if 'steps' not in tutorial or not isinstance(tutorial['steps'], list):
        ui.display_error("This tutorial has an invalid structure or no interactive steps.")
        return False
    
    steps = tutorial.get('steps', [])
    
    if not steps:
        ui.display_error("This tutorial has no interactive steps.")
        return False
    
    # Log the tutorial source
    tutorial_id = tutorial.get('id', 'Unknown')
    tutorial_title = tutorial.get('title', 'Unknown')
    
    # Default source is API
    source = "API"
    
    # Check for fromLocalFile flag (which shouldn't be present anymore)
    if tutorial.get('fromLocalFile'):
        source = "local file"
        logger.warning(f"Tutorial loaded from local file: {tutorial.get('localPath', 'Unknown')}")
    
    # Log successful tutorial load
    logger.info(f"Valid tutorial loaded from {source}: ID={tutorial_id}, Title={tutorial_title}, Steps={len(steps)}")
    ui.console.print(f"[green]Using tutorial from {source}: [bold]{tutorial_title}[/bold] ({tutorial_id})[/green]")
    
    return True


def run_animated_tutorial_step(ui: AnimatedTerminalUI, step: Dict[str, Any], 
                            step_number: int, total_steps: int, tutorial_id: str) -> bool:
    """
    Run an interactive tutorial step with animations.
    
    Args:
        ui: The animated terminal UI
        step: The step data
        step_number: The step number to display
        total_steps: Total number of steps in the tutorial
        tutorial_id: ID of the current tutorial
        
    Returns:
        bool: True if the step was completed successfully, False otherwise
    """
    if not step:
        ui.display_error("This step is empty or not available.")
        return False
    
    # Extract step details
    step_id = step.get('id', f'step{step_number}')
    expected_command = step.get('expectedCommand', '')
    hint = step.get('hint', 'Try reading the instructions carefully.')
    ascii_art = step.get('ascii_art', None)
    
    # Look for success_message and failure_message (newer format) 
    # These might be embedded in the hint field
    success_message = None
    failure_message = None
    
    # Extract success/failure messages from hint if they appear to be there
    if hint and "✅" in hint:
        # Try to extract the success message
        for line in hint.split("\n"):
            if "✅" in line:
                success_message = line.strip()
                break
                
    if hint and "❌" in hint:
        # Try to extract the failure message 
        for line in hint.split("\n"):
            if "❌" in line:
                failure_message = line.strip()
                break
    
    # Get validation settings
    validation = step.get('validation', {})
    if isinstance(validation, dict):
        validation_type = validation.get('type', 'exact')
        case_sensitive = validation.get('caseSensitive', False)
        pattern = validation.get('pattern', None)
        output_check = validation.get('outputMatch', None)
    else:
        # Fallback for older format
        validation_type = 'exact'
        case_sensitive = False
        pattern = None
        output_check = None
    
    # Handle regex validation pattern
    if validation_type == 'regex' and pattern:
        # Use the pattern for validation
        import re
        validation_type = 'custom_regex'
    
    # Display step with animations
    ui.display_step(step, step_number, total_steps)
    
    # Set up for attempts
    max_attempts = 3
    attempts = 0
    success = False
    
    # Command input loop with animations
    while attempts < max_attempts and not success:
        # Get command input with animation
        user_input = ui.get_command_input(step)
        
        # Check for special commands
        if user_input.lower() == 'exit' or user_input.lower() == 'quit':
            return False
        elif user_input.lower() == 'hint':
            ui.display_hint(hint)
            continue
        elif user_input.lower() == 'skip':
            ui.display_expected_command(expected_command)
            return True
        
        # Special handling for regex validation
        if validation_type == 'custom_regex' and pattern:
            import re
            try:
                is_correct = bool(re.match(pattern, user_input, re.IGNORECASE if not case_sensitive else 0))
                feedback = success_message if is_correct else (failure_message or "That's not quite right. Try again!")
            except Exception as e:
                logger.error(f"Error in regex validation: {str(e)}")
                is_correct = False
                feedback = "There was an error validating your command. Try again or ask for a hint."
        else:
            # Standard validation
            is_correct, feedback = check_command(
                user_input, 
                expected_command, 
                validation_type, 
                case_sensitive, 
                output_check
            )
            
            # Use custom success/failure messages if available
            if is_correct and success_message:
                feedback = success_message
            elif not is_correct and failure_message:
                feedback = failure_message
        
        # Display animated feedback
        ui.display_feedback(is_correct, feedback)
        
        if is_correct:
            success = True
            
            # Report step completion to the API
            tutorial_client = TutorialClient()
            tutorial_client.report_step_completion(tutorial_id, step_id, step.get('xpReward', step.get('xp', 10)))
            
            # Show XP animation
            ui.animate_xp_gain(step.get('xpReward', step.get('xp', 10)))
            
            # Display ASCII art if available
            if ascii_art:
                ui.console.print(ascii_art, style="bold green")
                time.sleep(1)
            
            # Celebrate step completion with animation
            ui.celebrate_step_completion()
        else:
            attempts += 1
            
            # If max attempts reached, show the expected command
            if attempts >= max_attempts:
                ui.display_expected_command(expected_command)
                
                # Ask if the user wants to continue
                ui.console.print()
                ui.console.print("[yellow]Would you like to continue to the next step? (y/n)[/yellow]")
                
                # Get user choice with animated prompt
                user_choice = input("> ").strip().lower()
                
                if user_choice != 'y' and user_choice != 'yes':
                    return False
                
                # Slight pause before continuing
                time.sleep(0.5)
                return True
    
    return True


def run_animated_tutorial(ui: AnimatedTerminalUI, tutorial: Dict[str, Any]) -> None:
    """
    Run an interactive tutorial with all its steps and animations.
    
    Args:
        ui: The animated terminal UI
        tutorial: The tutorial data
    """
    if not check_tutorial_validity(ui, tutorial):
        return
    
    # Display tutorial header with animation
    ui.display_tutorial_header(tutorial)
    
    # Add a slight pause before starting the tutorial
    time.sleep(1)
    
    # Log the tutorial being run
    logger.info(f"Running tutorial: ID={tutorial.get('id', 'Unknown')}, Title={tutorial.get('title', 'Unknown')}")
    
    total_xp = 0
    
    # Run each step with animations
    for i, step in enumerate(tutorial.get('steps', []), 1):
        # Ensure step is a dictionary
        if not isinstance(step, dict):
            ui.display_error(f"Step {i} has an invalid format. Skipping.")
            continue
            
        # Ensure step has the expected keys
        if 'instructions' not in step:
            step['instructions'] = f"Step {i} of the tutorial."
            
        if 'expectedCommand' not in step:
            step['expectedCommand'] = "Get-Help"
            
        if 'hint' not in step:
            step['hint'] = "Type the command shown in the instructions."
        
        if not run_animated_tutorial_step(ui, step, i, len(tutorial.get('steps', [])), tutorial.get('id')):
            # Step failed or user quit
            ui.display_error("Tutorial stopped. You can try again later.")
            return
        
        # Add XP for the step
        step_xp = step.get('xpReward', step.get('xp', 10))
        total_xp += step_xp
    
    # Report tutorial completion to the API
    tutorial_client = TutorialClient()
    tutorial_client.report_tutorial_completion(tutorial.get('id'), total_xp)
    
    # Celebrate tutorial completion with animations
    ui.celebrate_tutorial_completion(tutorial.get('title', 'Tutorial'), total_xp)


def animated_main():
    """Enhanced main entry point with animated UI for the CLI application."""
    # Initialize the animated UI
    ui = AnimatedTerminalUI()
    
    # Clear screen and show welcome message with animations
    ui.clear_screen()
    ui.show_welcome_screen()
    
    # Header animation
    ui.console.rule("[bold blue]CmdShiftLearn CLI Agent[/bold blue]")
    
    # Authenticate with API key animation
    ui.console.print()
    ui.animated_rich_text("Authenticating...", style="bold blue", delay=0.02)
    
    success, api_key, error = login()
    if not success or not api_key:
        ui.display_error("Authentication required to use CmdShiftLearn CLI.")
        if error:
            ui.console.print(f"[red]{error}[/red]")
        sys.exit(1)
    
    # Show connection info animation
    ui.animated_rich_text(
        f"Connecting to CmdShiftLearn API at: {API_BASE_URL}", 
        style="italic cyan", 
        delay=0.01
    )
    ui.console.print()
    
    # Create tutorial client with the API key
    tutorial_client = TutorialClient(api_key)
    
    try:
        # Fetch tutorials with loading animation
        ui.console.print("[bold]Fetching tutorials from API...[/bold]")
        tutorials = ui.display_loading(
            "Fetching tutorials from the API...",
            tutorial_client.get_tutorials
        )
        
        if not tutorials:
            ui.display_error("Failed to retrieve tutorials from the API. Please check your API configuration.")
            sys.exit(1)
        
        # Log the tutorials received
        logger.info(f"Fetched {len(tutorials)} tutorials from API")
        for tutorial in tutorials:
            logger.info(f"Tutorial: ID={tutorial.get('id', 'Unknown')}, Title={tutorial.get('title', 'Unknown')}")
        
        # Try to find tutorial_01_first_steps by default
        default_tutorial = None
        default_tutorial_id = "tutorial_01_first_steps"
        
        for tutorial in tutorials:
            if tutorial.get('id') == default_tutorial_id:
                default_tutorial = tutorial
                break
        
        if default_tutorial:
            logger.info(f"Found default tutorial: {default_tutorial.get('id')}")
            ui.console.print(f"[green]Found default tutorial: [bold]{default_tutorial.get('title')}[/bold][/green]")
            ui.console.print("[cyan]Press Enter to start this tutorial or select another one.[/cyan]")
        
        # Display available tutorials with animation
        ui.console.rule("[bold blue]Available Tutorials[/bold blue]")
        ui.console.print()
        
        # Create and display tutorial table with animations
        if default_tutorial:
            table_title = "Choose a tutorial or press Enter to start the default tutorial"
        else:
            table_title = "Choose a tutorial to begin your learning journey"
            
        ui.animated_rich_text(table_title, style="bold", delay=0.01)
        ui.console.print()
        
        # Display tutorials one by one with a subtle animation
        for i, tutorial in enumerate(tutorials, 1):
            title = tutorial.get('title', 'Untitled')
            difficulty = tutorial.get('difficulty', 'Unknown')
            id = tutorial.get('id', 'unknown')
            
            # Create a colorful display based on difficulty
            if difficulty.lower() == 'beginner':
                diff_color = "green"
            elif difficulty.lower() == 'intermediate':
                diff_color = "yellow"
            else:
                diff_color = "red"
            
            # Mark default tutorial
            if tutorial.get('id') == default_tutorial_id:
                tutorial_text = f"[cyan]{i}.[/cyan] [bold][yellow]* DEFAULT *[/yellow] {title}[/bold] [[{diff_color}]{difficulty}[/{diff_color}]] (ID: {id})"
            else:
                tutorial_text = f"[cyan]{i}.[/cyan] [bold]{title}[/bold] [[{diff_color}]{difficulty}[/{diff_color}]] (ID: {id})"
                
            ui.animated_rich_text(tutorial_text, delay=0.01)
            time.sleep(0.1)  # Slight pause between tutorials
        
        # Prompt for tutorial selection with animation
        ui.console.print()
        if default_tutorial:
            prompt_text = "[bold yellow]Enter the number of the tutorial you want to view (or press Enter for default):[/bold yellow]"
        else:
            prompt_text = "[bold yellow]Enter the number of the tutorial you want to view:[/bold yellow]"
            
        ui.animated_rich_text(prompt_text, delay=0.01)
        selection = input("> ").strip()
        
        # If no selection is made and we have a default tutorial, use that
        if not selection and default_tutorial:
            selected_tutorial = default_tutorial
            ui.console.print(f"[green]Starting default tutorial: [bold]{selected_tutorial.get('title')}[/bold][/green]")
            tutorial_id = selected_tutorial.get('id')
        else:
            try:
                index = int(selection) - 1
                if 0 <= index < len(tutorials):
                    selected_tutorial = tutorials[index]
                    tutorial_id = selected_tutorial.get('id')
                else:
                    ui.display_error(f"Invalid selection: {selection}. Please enter a number between 1 and {len(tutorials)}.")
                    # If we have a default tutorial, fall back to it
                    if default_tutorial:
                        ui.console.print(f"[yellow]Falling back to default tutorial: [bold]{default_tutorial.get('title')}[/bold][/yellow]")
                        selected_tutorial = default_tutorial
                        tutorial_id = selected_tutorial.get('id')
                    else:
                        # If no default tutorial, use the first one
                        ui.console.print(f"[yellow]Starting the first tutorial: [bold]{tutorials[0].get('title')}[/bold][/yellow]")
                        selected_tutorial = tutorials[0]
                        tutorial_id = selected_tutorial.get('id')
            except ValueError:
                ui.display_error(f"Invalid input: {selection}. Please enter a number.")
                # If we have a default tutorial, fall back to it
                if default_tutorial:
                    ui.console.print(f"[yellow]Falling back to default tutorial: [bold]{default_tutorial.get('title')}[/bold][/yellow]")
                    selected_tutorial = default_tutorial
                    tutorial_id = selected_tutorial.get('id')
                else:
                    # If no default tutorial, use the first one
                    ui.console.print(f"[yellow]Starting the first tutorial: [bold]{tutorials[0].get('title')}[/bold][/yellow]")
                    selected_tutorial = tutorials[0]
                    tutorial_id = selected_tutorial.get('id')
        
        # Loading animation for tutorial
        ui.console.print()
        loading_text = f"Loading tutorial [bold]{tutorial_id}[/bold]..."
        ui.animated_rich_text(loading_text, style="italic", delay=0.02)
        
        # Fetch the full tutorial details with loading animation
        full_tutorial = ui.display_loading(
            f"Fetching tutorial data from API...",
            lambda: tutorial_client.get_tutorial_by_id(tutorial_id)
        )
        
        if full_tutorial:
            # Log successful tutorial load
            logger.info(f"Successfully loaded tutorial from API: ID={tutorial_id}, Title={full_tutorial.get('title', 'Unknown')}")
            
            # Run the interactive tutorial with animations
            run_animated_tutorial(ui, full_tutorial)
        else:
            ui.display_error(f"Failed to load tutorial {tutorial_id} from API. Please check your connection.")
            sys.exit(1)
            
    except KeyboardInterrupt:
        ui.console.print("\n[bold yellow]Operation cancelled by user.[/bold yellow]")
        sys.exit(0)
    except Exception as e:
        ui.display_error(f"Unexpected error: {str(e)}")
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        sys.exit(1)


def main():
    """Main entry point dispatcher."""
    # Use the enhanced animated version by default
    animated_main()


if __name__ == "__main__":
    main()