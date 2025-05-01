#!/usr/bin/env python3
"""
Test script for authentication functionality
"""

import logging
from api.auth import login, signup

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('test_auth')

def test_login_and_signup():
    """Test login with invalid credentials and then signup"""
    email = "hello@cmdshiftlearn.com"
    password = "Password123"
    
    print(f"\nAttempting to login with: {email}")
    success, token, error_msg, error_details = login(email, password)
    
    print(f"Login success: {success}")
    print(f"Error message: {error_msg}")
    print(f"Error details: {error_details}")
    
    # Check if the error is due to invalid credentials
    is_invalid_credentials = (
        error_details and 
        (
            error_details.get('error') == 'invalid_credentials' or
            error_details.get('error_code') == 'invalid_credentials' or
            'invalid credentials' in error_details.get('error_description', '').lower() or
            'user not found' in error_details.get('error_description', '').lower()
        )
    )
    
    print(f"Is invalid credentials error: {is_invalid_credentials}")
    
    if is_invalid_credentials:
        print("\nAttempting to sign up...")
        signup_success, signup_token, signup_error = signup(email, password)
        
        print(f"Signup success: {signup_success}")
        if not signup_success:
            print(f"Signup error: {signup_error}")
        else:
            print("Signup successful! Now trying to login again...")
            login_success, login_token, login_error, _ = login(email, password)
            print(f"Login after signup success: {login_success}")

if __name__ == "__main__":
    test_login_and_signup()
