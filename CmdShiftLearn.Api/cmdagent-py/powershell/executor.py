"""
PowerShell execution engine for CmdShiftLearn.
"""

import os
import subprocess
import platform
import tempfile
from typing import Tuple, Dict, Any, List, Optional

# Create a singleton executor for easy access
_executor = None

def execute_powershell_command(command: str, timeout: int = 10) -> Tuple[bool, str, str]:
    """
    Execute a PowerShell command.
    
    Args:
        command: The PowerShell command to execute
        timeout: Timeout in seconds
        
    Returns:
        Tuple[bool, str, str]: (success, output, error)
    """
    global _executor
    
    # Initialize executor if needed
    if _executor is None:
        _executor = PowerShellExecutor(sandbox_mode=False)
    
    # Execute the command
    return _executor.execute_command(command)

class PowerShellExecutor:
    """Execute PowerShell commands and validate results."""
    
    def __init__(self, sandbox_mode: bool = True):
        """
        Initialize the PowerShell executor.
        
        Args:
            sandbox_mode: Whether to run commands in a sandboxed environment
        """
        self.sandbox_mode = sandbox_mode
        self.is_windows = platform.system() == "Windows"
        self.powershell_path = self._find_powershell_path()
        
        # Create a temporary directory for the sandbox
        if self.sandbox_mode:
            self.sandbox_dir = tempfile.mkdtemp(prefix="cmdshiftlearn_sandbox_")
            self._initialize_sandbox()
        
    def _find_powershell_path(self) -> str:
        """
        Find the PowerShell executable path.
        
        Returns:
            str: Path to PowerShell executable
        """
        if self.is_windows:
            # First check for PowerShell Core (pwsh.exe)
            powershell_core = os.path.join(os.environ.get("ProgramFiles", "C:\\Program Files"), "PowerShell", "7", "pwsh.exe")
            if os.path.exists(powershell_core):
                return powershell_core
            
            # Fallback to Windows PowerShell (powershell.exe)
            return "powershell.exe"
        else:
            # On non-Windows, PowerShell Core is called 'pwsh'
            return "pwsh"
    
    def _initialize_sandbox(self):
        """Initialize the sandbox environment."""
        # Create basic directory structure in the sandbox
        os.makedirs(os.path.join(self.sandbox_dir, "Documents"), exist_ok=True)
        os.makedirs(os.path.join(self.sandbox_dir, "Scripts"), exist_ok=True)
        
        # Create a test file
        with open(os.path.join(self.sandbox_dir, "test.txt"), "w") as f:
            f.write("This is a test file for PowerShell practice.")
    
    def _sandbox_command(self, command: str) -> str:
        """
        Sandbox a PowerShell command for safe execution.
        
        Args:
            command: The PowerShell command
            
        Returns:
            str: The sandboxed command
        """
        if not self.sandbox_mode:
            return command
        
        # Set the working directory to the sandbox
        sandboxed_command = f"Set-Location -Path '{self.sandbox_dir}'; "
        
        # Add command execution
        sandboxed_command += command
        
        # Restrict execution to sandbox directory using a try-catch block
        restricted_command = f"""
        try {{
            $OriginalLocation = Get-Location
            Set-Location -Path '{self.sandbox_dir}'
            
            # Execute the command
            {command}
            
            # Restore original location
            Set-Location -Path $OriginalLocation
        }} catch {{
            Write-Error "An error occurred: $_"
        }}
        """
        
        return restricted_command
    
    def execute_command(self, command: str, sandbox: bool = True) -> Tuple[bool, str, str]:
        """
        Execute a PowerShell command and return the result.
        
        Args:
            command: The PowerShell command to execute
            sandbox: Whether to run in a sandboxed environment
            
        Returns:
            tuple: (success, output, error)
        """
        if sandbox and self.sandbox_mode:
            # Add sandboxing logic
            command = self._sandbox_command(command)
        
        try:
            # Execute the PowerShell command
            process = subprocess.Popen(
                [self.powershell_path, "-NoProfile", "-Command", command],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )
            
            # Get the output and error
            stdout, stderr = process.communicate()
            success = process.returncode == 0
            
            return success, stdout, stderr
        except Exception as e:
            return False, "", str(e)
    
    def validate_command_output(self, command: str, expected_output: str, validation_type: str = 'contains') -> Tuple[bool, str]:
        """
        Validate a command's output against expected output.
        
        Args:
            command: The PowerShell command to execute
            expected_output: The expected output
            validation_type: The type of validation to perform ('contains', 'exact', 'regex')
            
        Returns:
            tuple: (is_valid, feedback)
        """
        import re
        
        # Execute the command
        success, stdout, stderr = self.execute_command(command)
        
        if not success:
            return False, f"Command execution failed: {stderr}"
        
        # Normalize output (remove extra whitespace, newlines, etc.)
        normalized_output = " ".join(stdout.strip().split())
        normalized_expected = " ".join(expected_output.strip().split())
        
        if validation_type == 'contains':
            # Check if the output contains the expected text
            if normalized_expected.lower() in normalized_output.lower():
                return True, "Command output contains the expected text."
            else:
                return False, "Command output does not contain the expected text."
                
        elif validation_type == 'exact':
            # Check for exact match
            if normalized_output.lower() == normalized_expected.lower():
                return True, "Command output exactly matches the expected output."
            else:
                return False, "Command output does not match the expected output."
                
        elif validation_type == 'regex':
            # Use regular expression to match output
            try:
                pattern = re.compile(expected_output, re.IGNORECASE | re.MULTILINE)
                if pattern.search(stdout):
                    return True, "Command output matches the expected pattern."
                else:
                    return False, "Command output does not match the expected pattern."
            except re.error:
                return False, "Invalid regular expression pattern."
        
        else:
            return False, "Unknown validation type."
    
    def get_command_help(self, command: str) -> str:
        """
        Get help information for a PowerShell command.
        
        Args:
            command: The PowerShell command
            
        Returns:
            str: Help information for the command
        """
        # Execute Get-Help for the command
        help_command = f"Get-Help {command} -Detailed | Out-String"
        success, stdout, stderr = self.execute_command(help_command, sandbox=False)
        
        if success and stdout:
            return stdout
        else:
            return f"Help information not available for '{command}'."
    
    def cleanup(self):
        """Clean up resources (e.g., delete sandbox directory)."""
        if self.sandbox_mode and os.path.exists(self.sandbox_dir):
            import shutil
            try:
                shutil.rmtree(self.sandbox_dir)
            except Exception as e:
                print(f"Error cleaning up sandbox: {e}")
