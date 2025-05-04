"""
Input handler for processing user commands in the terminal.
"""

import re
from typing import Dict, Any, Tuple

class InputHandler:
    """Handles input processing and validation for PowerShell commands."""
    
    def __init__(self):
        """Initialize the input handler."""
        # Common PowerShell aliases and their full commands
        self.aliases = {
            'gci': 'Get-ChildItem',
            'dir': 'Get-ChildItem',
            'ls': 'Get-ChildItem',
            'cd': 'Set-Location',
            'pwd': 'Get-Location',
            'echo': 'Write-Output',
            'cat': 'Get-Content',
            'type': 'Get-Content',
            'rm': 'Remove-Item',
            'del': 'Remove-Item',
            'cp': 'Copy-Item',
            'copy': 'Copy-Item',
            'mv': 'Move-Item',
            'move': 'Move-Item',
            'ps': 'Get-Process',
            'kill': 'Stop-Process',
        }
    
    def normalize_command(self, command: str) -> str:
        """
        Normalize a PowerShell command by expanding aliases and removing extra whitespace.
        
        Args:
            command: The command to normalize
            
        Returns:
            str: The normalized command
        """
        # Remove extra whitespace
        command = command.strip()
        
        # Check for aliases at the beginning of the command
        parts = command.split(maxsplit=1)
        if parts and parts[0].lower() in self.aliases:
            if len(parts) > 1:
                return f"{self.aliases[parts[0].lower()]} {parts[1]}"
            else:
                return self.aliases[parts[0].lower()]
        
        return command
    
    def check_command(self, user_input: str, expected_command: str, validation_type: str = 'exact') -> Tuple[bool, str]:
        """
        Check if the user's input matches the expected command.
        
        Args:
            user_input: The user's input to check
            expected_command: The expected command
            validation_type: The type of validation to perform ('exact', 'fuzzy', 'regex', 'output')
            
        Returns:
            Tuple[bool, str]: (is_correct, feedback_message)
        """
        if not user_input or not expected_command:
            return False, "Invalid input or expected command."
        
        # Normalize commands
        user_cmd = self.normalize_command(user_input)
        expected_cmd = self.normalize_command(expected_command)
        
        if validation_type == 'exact':
            # Case-insensitive exact match
            is_correct = user_cmd.lower() == expected_cmd.lower()
            if is_correct:
                return True, "Correct! Well done."
            else:
                return False, "That's not the right command. Try again."
                
        elif validation_type == 'fuzzy':
            # Check if core components are present (more lenient)
            user_parts = set(re.findall(r'[\w-]+', user_cmd.lower()))
            expected_parts = set(re.findall(r'[\w-]+', expected_cmd.lower()))
            
            # Fuzzy match if at least 80% of the expected parts are in the user's command
            common_parts = user_parts.intersection(expected_parts)
            match_ratio = len(common_parts) / len(expected_parts) if expected_parts else 0
            
            if match_ratio >= 0.8:
                return True, "That looks correct! Good job."
            else:
                return False, "Your command is close but not quite right. Try again."
                
        elif validation_type == 'regex':
            # Regular expression match
            try:
                pattern = re.compile(expected_cmd, re.IGNORECASE)
                is_correct = bool(pattern.match(user_cmd))
                
                if is_correct:
                    return True, "Your command matches the pattern. Good job!"
                else:
                    return False, "Your command doesn't match the expected pattern. Try again."
            except re.error:
                return False, "Error in validation pattern. Please contact the administrator."
                
        elif validation_type == 'output':
            # This will be implemented by the PowerShell executor to validate based on command output
            # Placeholder for now
            return False, "Output validation not implemented yet."
            
        else:
            return False, "Unknown validation type."
    
    def parse_args(self, command: str) -> Dict[str, Any]:
        """
        Parse arguments from a PowerShell command.
        
        Args:
            command: The command to parse
            
        Returns:
            Dict[str, Any]: Dictionary of parsed arguments
        """
        # Extract command name and arguments
        args = {}
        parts = command.split()
        
        if not parts:
            return args
        
        # Store the command name
        args['command'] = parts[0]
        
        # Parse named parameters (like -Name "value")
        i = 1
        while i < len(parts):
            if parts[i].startswith('-'):
                param_name = parts[i][1:]  # Remove the dash
                
                # Check if there's a value for this parameter
                if i + 1 < len(parts) and not parts[i + 1].startswith('-'):
                    # Handle quoted values
                    if parts[i + 1].startswith('"') and not parts[i + 1].endswith('"'):
                        # Find the end of the quoted value
                        value = parts[i + 1]
                        j = i + 2
                        while j < len(parts) and not parts[j].endswith('"'):
                            value += f" {parts[j]}"
                            j += 1
                        
                        if j < len(parts):
                            value += f" {parts[j]}"
                            i = j
                        
                        # Remove surrounding quotes
                        if value.startswith('"') and value.endswith('"'):
                            value = value[1:-1]
                    else:
                        value = parts[i + 1]
                        i += 1
                else:
                    # Switch parameter (no value)
                    value = True
                
                args[param_name] = value
            i += 1
        
        return args
