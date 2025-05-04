"""
Validation module for PowerShell commands and outputs.
"""

import re
from typing import Dict, Any, Tuple, List, Optional

class PowerShellValidator:
    """Validate PowerShell commands and their outputs."""
    
    def __init__(self):
        """Initialize the PowerShell validator."""
        # Common PowerShell command patterns
        self.command_patterns = {
            'get-help': r'(?i)Get-Help\s+[\w-]+(?:\s+-\w+)*',
            'get-command': r'(?i)Get-Command(?:\s+-\w+\s+[\w*-]+)?(?:\s+-\w+)*',
            'get-process': r'(?i)Get-Process(?:\s+-\w+\s+[\w-]+)?(?:\s+-\w+)*',
            'get-service': r'(?i)Get-Service(?:\s+-\w+\s+[\w-]+)?(?:\s+-\w+)*',
            'get-childitem': r'(?i)(?:Get-ChildItem|gci|dir|ls)(?:\s+-\w+\s+[\w-]+)?(?:\s+-\w+)*',
            'new-item': r'(?i)New-Item(?:\s+-\w+\s+[\w-]+)?(?:\s+-\w+)*',
            'set-location': r'(?i)(?:Set-Location|cd|chdir)\s+[\w\\/.:-]+',
            'get-content': r'(?i)(?:Get-Content|cat|type)\s+[\w\\/.:-]+(?:\s+-\w+)*',
        }
    
    def validate_command_syntax(self, command: str) -> Tuple[bool, str]:
        """
        Validate the syntax of a PowerShell command.
        
        Args:
            command: The PowerShell command to validate
            
        Returns:
            tuple: (is_valid, feedback)
        """
        # Check if the command is empty
        if not command.strip():
            return False, "The command is empty."
        
        # Extract the command name (first word)
        command_parts = command.strip().split(maxsplit=1)
        command_name = command_parts[0].lower() if command_parts else ""
        
        # Check common PowerShell verbs and nouns
        valid_verbs = ['get', 'set', 'new', 'remove', 'start', 'stop', 'add', 'clear', 'copy',
                      'export', 'import', 'invoke', 'measure', 'move', 'out', 'read', 'restart',
                      'select', 'sort', 'test', 'write']
        
        # Check if the command follows PowerShell naming convention (Verb-Noun)
        if '-' in command_name:
            verb, noun = command_name.split('-', 1)
            if verb.lower() in valid_verbs:
                return True, "Command follows PowerShell verb-noun convention."
        
        # Check against known command patterns
        for pattern_name, pattern in self.command_patterns.items():
            if re.match(pattern, command):
                return True, f"Command matches {pattern_name} pattern."
        
        # If we couldn't validate the command, but it doesn't look invalid
        return True, "Command syntax is acceptable."
    
    def validate_command_against_expected(self, command: str, expected_command: str, 
                                         validation_type: str = 'exact') -> Tuple[bool, str]:
        """
        Validate a command against an expected command.
        
        Args:
            command: The command to validate
            expected_command: The expected command
            validation_type: The type of validation ('exact', 'fuzzy', 'pattern')
            
        Returns:
            tuple: (is_valid, feedback)
        """
        # Normalize commands (trim whitespace, etc.)
        command = command.strip()
        expected_command = expected_command.strip()
        
        if validation_type == 'exact':
            # Case-insensitive exact match
            if command.lower() == expected_command.lower():
                return True, "Command matches exactly."
            else:
                return False, "Command does not match the expected command."
                
        elif validation_type == 'fuzzy':
            # Check for approximate match
            command_parts = re.findall(r'[\w-]+', command.lower())
            expected_parts = re.findall(r'[\w-]+', expected_command.lower())
            
            # Calculate how many parts match
            common_parts = set(command_parts).intersection(set(expected_parts))
            if not expected_parts:
                return False, "Invalid expected command."
                
            match_ratio = len(common_parts) / len(expected_parts)
            
            if match_ratio >= 0.8:
                return True, "Command is close enough to the expected command."
            else:
                return False, "Command is too different from the expected command."
                
        elif validation_type == 'pattern':
            # Treat expected_command as a regex pattern
            try:
                pattern = re.compile(expected_command, re.IGNORECASE)
                if pattern.match(command):
                    return True, "Command matches the expected pattern."
                else:
                    return False, "Command does not match the expected pattern."
            except re.error:
                return False, "Invalid pattern for command validation."
        
        else:
            return False, "Unknown validation type."
    
    def extract_parameters(self, command: str) -> Dict[str, str]:
        """
        Extract parameters from a PowerShell command.
        
        Args:
            command: The PowerShell command
            
        Returns:
            dict: Dictionary of parameter names and values
        """
        parameters = {}
        
        # Match parameter patterns like -Name Value or -Name "Value with spaces"
        param_pattern = r'-(\w+)(?:\s+([^-"][^\s]*)|(?:\s+)?"([^"]*)")?'
        matches = re.finditer(param_pattern, command)
        
        for match in matches:
            param_name = match.group(1)
            # Check which capture group contains the value (if any)
            if match.group(3):
                # Quoted value
                param_value = match.group(3)
            elif match.group(2):
                # Unquoted value
                param_value = match.group(2)
            else:
                # Switch parameter (no value)
                param_value = True
            
            parameters[param_name] = param_value
        
        return parameters
    
    def analyze_command_complexity(self, command: str) -> Dict[str, Any]:
        """
        Analyze the complexity of a PowerShell command.
        
        Args:
            command: The PowerShell command
            
        Returns:
            dict: Analysis results with complexity metrics
        """
        analysis = {
            'complexity': 'basic',
            'components': [],
            'features_used': [],
            'feedback': '',
        }
        
        # Check for simple command (just verb-noun)
        if len(command.strip().split()) == 1:
            analysis['complexity'] = 'basic'
            analysis['feedback'] = 'This is a basic command without parameters.'
            return analysis
        
        # Check for parameters
        parameters = self.extract_parameters(command)
        if parameters:
            analysis['components'].append('parameters')
            analysis['features_used'].extend(list(parameters.keys()))
        
        # Check for pipeline usage
        if '|' in command:
            analysis['components'].append('pipeline')
            analysis['complexity'] = 'intermediate'
            
            # Count pipeline stages
            pipeline_stages = [stage.strip() for stage in command.split('|') if stage.strip()]
            analysis['pipeline_stages'] = len(pipeline_stages)
            
            if len(pipeline_stages) > 2:
                analysis['complexity'] = 'advanced'
        
        # Check for control structures (if, foreach, etc.)
        control_patterns = [
            (r'if\s*\(.*\)', 'conditional'),
            (r'foreach\s*\(.*\)', 'loop'),
            (r'while\s*\(.*\)', 'loop'),
            (r'switch\s*\(.*\)', 'switch'),
            (r'\$.*=.*', 'variable'),
            (r'function\s+\w+', 'function'),
        ]
        
        for pattern, component in control_patterns:
            if re.search(pattern, command, re.IGNORECASE):
                analysis['components'].append(component)
                analysis['complexity'] = 'advanced'
        
        # Generate feedback based on analysis
        if analysis['complexity'] == 'basic':
            analysis['feedback'] = 'This is a basic command with simple parameters.'
        elif analysis['complexity'] == 'intermediate':
            analysis['feedback'] = 'This is an intermediate command using pipelines or multiple parameters.'
        else:
            analysis['feedback'] = 'This is an advanced command using control structures, functions, or complex pipelines.'
        
        return analysis
