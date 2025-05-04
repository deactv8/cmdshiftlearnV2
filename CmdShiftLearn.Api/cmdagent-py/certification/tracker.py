"""
Certification tracking system for CmdShiftLearn.
"""

import os
import yaml
from typing import Dict, List, Any, Optional
from pathlib import Path

from utils.config import DATA_DIR


class CertificationTracker:
    """Track and manage progress towards certifications."""
    
    def __init__(self, content_dir: str = None):
        """
        Initialize the certification tracker.
        
        Args:
            content_dir: Directory containing certification data (defaults to data/content/certifications)
        """
        self.content_dir = Path(content_dir) if content_dir else Path(DATA_DIR) / "content" / "certifications"
        self.certifications = {}
        self.load_certifications()
    
    def load_certifications(self):
        """Load certification data from files."""
        if not self.content_dir.exists():
            os.makedirs(self.content_dir, exist_ok=True)
            
            # Create default certification files if directory is empty
            if not list(self.content_dir.glob("*.yaml")):
                self._create_default_certifications()
                
        # Load certification files
        for file_path in self.content_dir.glob("*.yaml"):
            try:
                with open(file_path, 'r') as file:
                    cert_data = yaml.safe_load(file)
                    if cert_data and 'id' in cert_data:
                        self.certifications[cert_data['id']] = cert_data
            except Exception as e:
                print(f"Error loading certification data from {file_path}: {e}")
    
    def _create_default_certifications(self):
        """Create default certification files."""
        # AZ-104 certification
        az104 = {
            'id': 'az104',
            'title': 'Microsoft Azure Administrator (AZ-104)',
            'description': 'Learn to implement, manage, and monitor an organization\'s Microsoft Azure environment.',
            'domains': [
                {
                    'id': 'identity',
                    'name': 'Manage Azure identities and governance',
                    'weight': 20,
                    'skills': [
                        'Manage Azure Active Directory objects',
                        'Manage role-based access control',
                        'Manage subscriptions and governance'
                    ]
                },
                {
                    'id': 'storage',
                    'name': 'Implement and manage storage',
                    'weight': 15,
                    'skills': [
                        'Configure storage accounts',
                        'Configure Azure Files and Azure Blob Storage',
                        'Manage access keys'
                    ]
                },
                {
                    'id': 'compute',
                    'name': 'Deploy and manage Azure compute resources',
                    'weight': 20,
                    'skills': [
                        'Configure VMs for high availability',
                        'Automate deployment of VMs',
                        'Configure Azure App Service',
                        'Configure Azure Container Instances'
                    ]
                },
                {
                    'id': 'networking',
                    'name': 'Configure and manage virtual networking',
                    'weight': 25,
                    'skills': [
                        'Implement virtual networking',
                        'Configure private and public IP addresses',
                        'Configure Azure DNS',
                        'Configure Azure Firewall'
                    ]
                },
                {
                    'id': 'monitoring',
                    'name': 'Monitor and maintain Azure resources',
                    'weight': 20,
                    'skills': [
                        'Configure Azure Monitor',
                        'Configure alerts and action groups',
                        'Apply and maintain governance'
                    ]
                }
            ],
            'externalLinks': [
                {
                    'title': 'Official Microsoft AZ-104 Page',
                    'url': 'https://learn.microsoft.com/en-us/certifications/azure-administrator/'
                },
                {
                    'title': 'Microsoft Learn Path',
                    'url': 'https://learn.microsoft.com/en-us/training/paths/az-104-administrator-prerequisites/'
                }
            ],
            'relatedTutorials': [],
            'relatedChallenges': []
        }
        
        # SC-300 certification
        sc300 = {
            'id': 'sc300',
            'title': 'Microsoft Identity and Access Administrator (SC-300)',
            'description': 'Learn to design, implement, and operate an organization\'s identity and access management systems.',
            'domains': [
                {
                    'id': 'identity',
                    'name': 'Implement an identity management solution',
                    'weight': 25,
                    'skills': [
                        'Implement initial configuration of Azure AD',
                        'Create, configure, and manage identities',
                        'Implement and manage external identities',
                        'Implement and manage hybrid identity'
                    ]
                },
                {
                    'id': 'authentication',
                    'name': 'Implement an authentication and access management solution',
                    'weight': 35,
                    'skills': [
                        'Plan and implement Azure MFA',
                        'Manage user authentication',
                        'Plan, implement, and administer conditional access',
                        'Manage Azure AD Identity Protection'
                    ]
                },
                {
                    'id': 'governance',
                    'name': 'Implement access management for apps',
                    'weight': 20,
                    'skills': [
                        'Plan, implement, and monitor the integration of Enterprise Apps',
                        'Implement app registration',
                        'Configure Azure AD App Proxy'
                    ]
                },
                {
                    'id': 'roles',
                    'name': 'Plan and implement an identity governance strategy',
                    'weight': 20,
                    'skills': [
                        'Plan and implement entitlement management',
                        'Plan, implement, and manage access reviews',
                        'Plan and implement privileged access',
                        'Monitor and maintain Azure AD'
                    ]
                }
            ],
            'externalLinks': [
                {
                    'title': 'Official Microsoft SC-300 Page',
                    'url': 'https://learn.microsoft.com/en-us/certifications/identity-and-access-administrator/'
                },
                {
                    'title': 'Microsoft Learn Path',
                    'url': 'https://learn.microsoft.com/en-us/training/paths/sc-300-identity-management/'
                }
            ],
            'relatedTutorials': [],
            'relatedChallenges': []
        }
        
        # Save the default certifications
        self._save_certification(az104)
        self._save_certification(sc300)
    
    def _save_certification(self, cert_data: Dict[str, Any]) -> bool:
        """
        Save certification data to a file.
        
        Args:
            cert_data: Certification data to save
            
        Returns:
            bool: True if saved successfully, False otherwise
        """
        if not cert_data or 'id' not in cert_data:
            return False
            
        cert_id = cert_data['id']
        file_path = self.content_dir / f"{cert_id}.yaml"
        
        try:
            # Make sure the directory exists
            os.makedirs(os.path.dirname(file_path), exist_ok=True)
            
            # Save the certification data
            with open(file_path, 'w') as file:
                yaml.dump(cert_data, file, default_flow_style=False)
                
            # Update in-memory data
            self.certifications[cert_id] = cert_data
            
            return True
        except Exception as e:
            print(f"Error saving certification data: {e}")
            return False
    
    def get_certification(self, cert_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a certification by ID.
        
        Args:
            cert_id: Certification ID
            
        Returns:
            dict: Certification data or None if not found
        """
        return self.certifications.get(cert_id)
    
    def get_all_certifications(self) -> List[Dict[str, Any]]:
        """
        Get all certifications.
        
        Returns:
            list: List of all certifications
        """
        return list(self.certifications.values())
    
    def get_certification_domain(self, cert_id: str, domain_id: str) -> Optional[Dict[str, Any]]:
        """
        Get a specific domain from a certification.
        
        Args:
            cert_id: Certification ID
            domain_id: Domain ID
            
        Returns:
            dict: Domain data or None if not found
        """
        cert = self.get_certification(cert_id)
        if not cert:
            return None
            
        domains = cert.get('domains', [])
        for domain in domains:
            if domain.get('id') == domain_id:
                return domain
                
        return None
    
    def calculate_certification_progress(self, cert_id: str, completed_tutorials: List[str], 
                                       completed_challenges: List[str], quiz_scores: Dict[str, int] = None) -> Dict[str, Any]:
        """
        Calculate progress towards a certification.
        
        Args:
            cert_id: Certification ID
            completed_tutorials: List of completed tutorial IDs
            completed_challenges: List of completed challenge IDs
            quiz_scores: Dictionary mapping quiz IDs to scores
            
        Returns:
            dict: Certification progress data
        """
        cert = self.get_certification(cert_id)
        if not cert:
            return {'total_progress': 0, 'domains': {}}
            
        domains = cert.get('domains', [])
        if not domains:
            return {'total_progress': 0, 'domains': {}}
            
        # Progress data structure
        progress = {
            'domains': {},
            'total_progress': 0.0
        }
        
        # Calculate progress for each domain
        for domain in domains:
            domain_id = domain.get('id')
            domain_weight = domain.get('weight', 0)
            
            # Find related tutorials and challenges for this domain
            domain_tutorials = self._get_tutorials_for_domain(cert_id, domain_id)
            domain_challenges = self._get_challenges_for_domain(cert_id, domain_id)
            domain_quizzes = self._get_quizzes_for_domain(cert_id, domain_id)
            
            # Calculate completion ratios
            tutorial_ratio = self._calculate_completion_ratio(domain_tutorials, completed_tutorials)
            challenge_ratio = self._calculate_completion_ratio(domain_challenges, completed_challenges)
            quiz_ratio = self._calculate_quiz_ratio(domain_quizzes, quiz_scores)
            
            # Calculate domain progress (weighting can be adjusted)
            tutorial_weight = 0.4
            challenge_weight = 0.3
            quiz_weight = 0.3
            
            domain_progress = (tutorial_ratio * tutorial_weight + 
                             challenge_ratio * challenge_weight + 
                             quiz_ratio * quiz_weight) * 100
                             
            # Store domain progress
            progress['domains'][domain_id] = domain_progress
            
            # Add weighted contribution to total progress
            progress['total_progress'] += domain_progress * (domain_weight / 100)
        
        return progress
    
    def _get_tutorials_for_domain(self, cert_id: str, domain_id: str) -> List[str]:
        """
        Get tutorials related to a certification domain.
        
        Args:
            cert_id: Certification ID
            domain_id: Domain ID
            
        Returns:
            list: List of tutorial IDs
        """
        # This would typically query a mapping of tutorials to certification domains
        # For now, we'll return a placeholder list
        cert = self.get_certification(cert_id)
        if not cert:
            return []
            
        # Get tutorials related to this certification
        related_tutorials = cert.get('relatedTutorials', [])
        
        # In a real implementation, we would filter by domain
        # For now, just return all related tutorials
        return related_tutorials
    
    def _get_challenges_for_domain(self, cert_id: str, domain_id: str) -> List[str]:
        """
        Get challenges related to a certification domain.
        
        Args:
            cert_id: Certification ID
            domain_id: Domain ID
            
        Returns:
            list: List of challenge IDs
        """
        # Similar to _get_tutorials_for_domain
        cert = self.get_certification(cert_id)
        if not cert:
            return []
            
        # Get challenges related to this certification
        related_challenges = cert.get('relatedChallenges', [])
        
        # In a real implementation, we would filter by domain
        # For now, just return all related challenges
        return related_challenges
    
    def _get_quizzes_for_domain(self, cert_id: str, domain_id: str) -> List[str]:
        """
        Get quizzes related to a certification domain.
        
        Args:
            cert_id: Certification ID
            domain_id: Domain ID
            
        Returns:
            list: List of quiz IDs
        """
        # Placeholder - in a real implementation, this would query a database
        return []
    
    def _calculate_completion_ratio(self, domain_items: List[str], completed_items: List[str]) -> float:
        """
        Calculate completion ratio for a list of items.
        
        Args:
            domain_items: List of items for a domain
            completed_items: List of completed items
            
        Returns:
            float: Completion ratio (0-1)
        """
        if not domain_items:
            return 0.0
            
        # Count completed items in this domain
        completed_count = sum(1 for item in domain_items if item in completed_items)
        
        return completed_count / len(domain_items) if domain_items else 0.0
    
    def _calculate_quiz_ratio(self, domain_quizzes: List[str], quiz_scores: Dict[str, int] = None) -> float:
        """
        Calculate quiz score ratio for a domain.
        
        Args:
            domain_quizzes: List of quizzes for a domain
            quiz_scores: Dictionary mapping quiz IDs to scores
            
        Returns:
            float: Quiz score ratio (0-1)
        """
        if not domain_quizzes or not quiz_scores:
            return 0.0
            
        # Sum scores for quizzes in this domain
        total_score = 0
        max_score = 0
        count = 0
        
        for quiz_id in domain_quizzes:
            if quiz_id in quiz_scores:
                total_score += quiz_scores[quiz_id]
                max_score += 100  # Assuming maximum score is 100
                count += 1
                
        return total_score / max_score if max_score > 0 else 0.0
    
    def link_tutorial_to_certification(self, tutorial_id: str, cert_id: str, domain_id: str = None) -> bool:
        """
        Link a tutorial to a certification.
        
        Args:
            tutorial_id: Tutorial ID
            cert_id: Certification ID
            domain_id: Optional domain ID
            
        Returns:
            bool: True if linked successfully, False otherwise
        """
        cert = self.get_certification(cert_id)
        if not cert:
            return False
            
        # Add tutorial to certification's related tutorials if not already there
        related_tutorials = cert.get('relatedTutorials', [])
        if tutorial_id not in related_tutorials:
            related_tutorials.append(tutorial_id)
            cert['relatedTutorials'] = related_tutorials
            
            # Save changes
            return self._save_certification(cert)
        
        return True
    
    def link_challenge_to_certification(self, challenge_id: str, cert_id: str, domain_id: str = None) -> bool:
        """
        Link a challenge to a certification.
        
        Args:
            challenge_id: Challenge ID
            cert_id: Certification ID
            domain_id: Optional domain ID
            
        Returns:
            bool: True if linked successfully, False otherwise
        """
        cert = self.get_certification(cert_id)
        if not cert:
            return False
            
        # Add challenge to certification's related challenges if not already there
        related_challenges = cert.get('relatedChallenges', [])
        if challenge_id not in related_challenges:
            related_challenges.append(challenge_id)
            cert['relatedChallenges'] = related_challenges
            
            # Save changes
            return self._save_certification(cert)
        
        return True
    
    def get_learning_path(self, cert_id: str) -> List[Dict[str, Any]]:
        """
        Get a recommended learning path for a certification.
        
        Args:
            cert_id: Certification ID
            
        Returns:
            list: List of learning path items
        """
        cert = self.get_certification(cert_id)
        if not cert:
            return []
            
        learning_path = []
        
        # Add intro item
        learning_path.append({
            'type': 'intro',
            'title': f"Introduction to {cert.get('title')}",
            'description': cert.get('description'),
            'duration': 15  # minutes
        })
        
        # Add domain items
        for domain in cert.get('domains', []):
            domain_id = domain.get('id')
            domain_name = domain.get('name')
            domain_weight = domain.get('weight', 0)
            
            # Add domain header
            learning_path.append({
                'type': 'domain',
                'id': domain_id,
                'title': domain_name,
                'weight': domain_weight,
                'description': f"Learn skills for the {domain_name} domain"
            })
            
            # Add tutorials for this domain
            tutorials = self._get_tutorials_for_domain(cert_id, domain_id)
            for tutorial_id in tutorials:
                learning_path.append({
                    'type': 'tutorial',
                    'id': tutorial_id,
                    'domain_id': domain_id,
                    'title': f"Tutorial: {tutorial_id}",  # Title would be populated from actual tutorial data
                    'description': "Complete this tutorial to learn necessary skills"
                })
            
            # Add challenges for this domain
            challenges = self._get_challenges_for_domain(cert_id, domain_id)
            for challenge_id in challenges:
                learning_path.append({
                    'type': 'challenge',
                    'id': challenge_id,
                    'domain_id': domain_id,
                    'title': f"Challenge: {challenge_id}",  # Title would be populated from actual challenge data
                    'description': "Complete this challenge to practice your skills"
                })
                
            # Add quiz for this domain
            learning_path.append({
                'type': 'quiz',
                'id': f"{cert_id}_{domain_id}_quiz",
                'domain_id': domain_id,
                'title': f"{domain_name} Quiz",
                'description': f"Test your knowledge of {domain_name}"
            })
        
        # Add final exam prep
        learning_path.append({
            'type': 'exam_prep',
            'title': f"{cert.get('title')} Exam Preparation",
            'description': "Final preparation for the certification exam",
            'duration': 60  # minutes
        })
        
        return learning_path
