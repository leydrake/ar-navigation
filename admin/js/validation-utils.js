// Centralized Validation and Error Handling Utilities
class ValidationUtils {
    constructor() {
        this.errorMessages = {
            required: 'This field is required',
            email: 'Please enter a valid email address',
            password: 'Password must be at least 6 characters long',
            passwordMatch: 'Passwords do not match',
            phone: 'Please enter a valid phone number',
            url: 'Please enter a valid URL',
            number: 'Please enter a valid number',
            minLength: (min) => `Must be at least ${min} characters long`,
            maxLength: (max) => `Must be no more than ${max} characters long`,
            min: (min) => `Must be at least ${min}`,
            max: (max) => `Must be no more than ${max}`,
            date: 'Please enter a valid date',
            time: 'Please enter a valid time',
            futureDate: 'Date must be in the future',
            pastDate: 'Date must be in the past',
            endAfterStart: 'End time must be after start time',
            fileSize: (maxSize) => `File size must be less than ${maxSize}MB`,
            fileType: (types) => `File type must be one of: ${types.join(', ')}`,
            imageSize: (maxWidth, maxHeight) => `Image dimensions must be less than ${maxWidth}x${maxHeight}px`
        };
    }

    // Email validation
    validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    // Password validation
    validatePassword(password, minLength = 6) {
        return password && password.length >= minLength;
    }

    // Phone validation
    validatePhone(phone) {
        const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
        return phoneRegex.test(phone.replace(/[\s\-\(\)]/g, ''));
    }

    // URL validation
    validateURL(url) {
        try {
            new URL(url);
            return true;
        } catch {
            return false;
        }
    }

    // Number validation
    validateNumber(value, min = null, max = null) {
        const num = parseFloat(value);
        if (isNaN(num)) return false;
        if (min !== null && num < min) return false;
        if (max !== null && num > max) return false;
        return true;
    }

    // Date validation
    validateDate(dateString) {
        const date = new Date(dateString);
        return date instanceof Date && !isNaN(date);
    }

    // Time validation
    validateTime(timeString) {
        const timeRegex = /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/;
        return timeRegex.test(timeString);
    }

    // File validation
    validateFile(file, options = {}) {
        const { maxSize = 5, allowedTypes = ['image/jpeg', 'image/png', 'image/gif'] } = options;
        
        if (file.size > maxSize * 1024 * 1024) {
            return { valid: false, message: this.errorMessages.fileSize(maxSize) };
        }
        
        if (!allowedTypes.includes(file.type)) {
            return { valid: false, message: this.errorMessages.fileType(allowedTypes) };
        }
        
        return { valid: true };
    }

    // Image dimension validation
    validateImageDimensions(file, maxWidth = 2000, maxHeight = 2000) {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => {
                if (img.width > maxWidth || img.height > maxHeight) {
                    resolve({ valid: false, message: this.errorMessages.imageSize(maxWidth, maxHeight) });
                } else {
                    resolve({ valid: true });
                }
            };
            img.onerror = () => resolve({ valid: false, message: 'Invalid image file' });
            img.src = URL.createObjectURL(file);
        });
    }

    // Form validation
    validateForm(form, rules) {
        const errors = {};
        let isValid = true;

        for (const [fieldName, fieldRules] of Object.entries(rules)) {
            const field = form.querySelector(`[name="${fieldName}"]`);
            if (!field) continue;

            const value = field.value.trim();
            const fieldErrors = [];

            // Required validation
            if (fieldRules.required && !value) {
                fieldErrors.push(this.errorMessages.required);
                isValid = false;
            }

            // Skip other validations if field is empty and not required
            if (!value && !fieldRules.required) continue;

            // Email validation
            if (fieldRules.email && value && !this.validateEmail(value)) {
                fieldErrors.push(this.errorMessages.email);
                isValid = false;
            }

            // Password validation
            if (fieldRules.password && value && !this.validatePassword(value, fieldRules.minLength)) {
                fieldErrors.push(fieldRules.minLength ? 
                    this.errorMessages.minLength(fieldRules.minLength) : 
                    this.errorMessages.password);
                isValid = false;
            }

            // Password match validation
            if (fieldRules.passwordMatch && value) {
                const matchField = form.querySelector(`[name="${fieldRules.passwordMatch}"]`);
                if (matchField && value !== matchField.value) {
                    fieldErrors.push(this.errorMessages.passwordMatch);
                    isValid = false;
                }
            }

            // Phone validation
            if (fieldRules.phone && value && !this.validatePhone(value)) {
                fieldErrors.push(this.errorMessages.phone);
                isValid = false;
            }

            // URL validation
            if (fieldRules.url && value && !this.validateURL(value)) {
                fieldErrors.push(this.errorMessages.url);
                isValid = false;
            }

            // Number validation
            if (fieldRules.number && value) {
                if (!this.validateNumber(value, fieldRules.min, fieldRules.max)) {
                    if (fieldRules.min !== undefined && fieldRules.max !== undefined) {
                        fieldErrors.push(`Must be between ${fieldRules.min} and ${fieldRules.max}`);
                    } else if (fieldRules.min !== undefined) {
                        fieldErrors.push(this.errorMessages.min(fieldRules.min));
                    } else if (fieldRules.max !== undefined) {
                        fieldErrors.push(this.errorMessages.max(fieldRules.max));
                    } else {
                        fieldErrors.push(this.errorMessages.number);
                    }
                    isValid = false;
                }
            }

            // Date validation
            if (fieldRules.date && value && !this.validateDate(value)) {
                fieldErrors.push(this.errorMessages.date);
                isValid = false;
            }

            // Time validation
            if (fieldRules.time && value && !this.validateTime(value)) {
                fieldErrors.push(this.errorMessages.time);
                isValid = false;
            }

            // Future date validation
            if (fieldRules.futureDate && value) {
                const date = new Date(value);
                if (date <= new Date()) {
                    fieldErrors.push(this.errorMessages.futureDate);
                    isValid = false;
                }
            }

            // Past date validation
            if (fieldRules.pastDate && value) {
                const date = new Date(value);
                if (date >= new Date()) {
                    fieldErrors.push(this.errorMessages.pastDate);
                    isValid = false;
                }
            }

            // Length validation
            if (fieldRules.minLength && value.length < fieldRules.minLength) {
                fieldErrors.push(this.errorMessages.minLength(fieldRules.minLength));
                isValid = false;
            }

            if (fieldRules.maxLength && value.length > fieldRules.maxLength) {
                fieldErrors.push(this.errorMessages.maxLength(fieldRules.maxLength));
                isValid = false;
            }

            // Custom validation
            if (fieldRules.custom && typeof fieldRules.custom === 'function') {
                const customResult = fieldRules.custom(value, form);
                if (customResult !== true) {
                    fieldErrors.push(customResult || 'Invalid value');
                    isValid = false;
                }
            }

            if (fieldErrors.length > 0) {
                errors[fieldName] = fieldErrors;
            }
        }

        return { isValid, errors };
    }

    // Show field error
    showFieldError(input, message) {
        const formGroup = input.closest('.form-group') || input.closest('.field') || input.parentElement;
        formGroup.classList.add('error');
        formGroup.classList.remove('success');
        
        // Remove existing error message
        const existingError = formGroup.querySelector('.error-message');
        if (existingError) {
            existingError.remove();
        }
        
        const errorMsg = document.createElement('div');
        errorMsg.className = 'error-message';
        errorMsg.textContent = message;
        formGroup.appendChild(errorMsg);
        
        // Add error styling to input
        input.classList.add('error');
        input.classList.remove('success');
    }

    // Show field success
    showFieldSuccess(input) {
        const formGroup = input.closest('.form-group') || input.closest('.field') || input.parentElement;
        formGroup.classList.add('success');
        formGroup.classList.remove('error');
        
        // Remove existing error message
        const existingError = formGroup.querySelector('.error-message');
        if (existingError) {
            existingError.remove();
        }
        
        // Add success styling to input
        input.classList.add('success');
        input.classList.remove('error');
    }

    // Clear field validation
    clearFieldValidation(input) {
        const formGroup = input.closest('.form-group') || input.closest('.field') || input.parentElement;
        formGroup.classList.remove('error', 'success');
        
        const existingError = formGroup.querySelector('.error-message');
        if (existingError) {
            existingError.remove();
        }
        
        input.classList.remove('error', 'success');
    }

    // Clear all form validation
    clearFormValidation(form) {
        const fields = form.querySelectorAll('input, select, textarea');
        fields.forEach(field => this.clearFieldValidation(field));
    }

    // Show form errors
    showFormErrors(form, errors) {
        this.clearFormValidation(form);
        
        for (const [fieldName, fieldErrors] of Object.entries(errors)) {
            const field = form.querySelector(`[name="${fieldName}"]`);
            if (field && fieldErrors.length > 0) {
                this.showFieldError(field, fieldErrors[0]); // Show first error
            }
        }
    }

    // Real-time validation
    addRealTimeValidation(form, rules) {
        for (const [fieldName, fieldRules] of Object.entries(rules)) {
            const field = form.querySelector(`[name="${fieldName}"]`);
            if (!field) continue;

            // Validate on blur
            field.addEventListener('blur', () => {
                const value = field.value.trim();
                
                if (!value && !fieldRules.required) {
                    this.clearFieldValidation(field);
                    return;
                }

                if (fieldRules.required && !value) {
                    this.showFieldError(field, this.errorMessages.required);
                    return;
                }

                if (fieldRules.email && value && !this.validateEmail(value)) {
                    this.showFieldError(field, this.errorMessages.email);
                    return;
                }

                if (fieldRules.password && value && !this.validatePassword(value, fieldRules.minLength)) {
                    this.showFieldError(field, fieldRules.minLength ? 
                        this.errorMessages.minLength(fieldRules.minLength) : 
                        this.errorMessages.password);
                    return;
                }

                if (fieldRules.passwordMatch && value) {
                    const matchField = form.querySelector(`[name="${fieldRules.passwordMatch}"]`);
                    if (matchField && value !== matchField.value) {
                        this.showFieldError(field, this.errorMessages.passwordMatch);
                        return;
                    }
                }

                if (fieldRules.phone && value && !this.validatePhone(value)) {
                    this.showFieldError(field, this.errorMessages.phone);
                    return;
                }

                if (fieldRules.url && value && !this.validateURL(value)) {
                    this.showFieldError(field, this.errorMessages.url);
                    return;
                }

                if (fieldRules.number && value) {
                    if (!this.validateNumber(value, fieldRules.min, fieldRules.max)) {
                        let message = this.errorMessages.number;
                        if (fieldRules.min !== undefined && fieldRules.max !== undefined) {
                            message = `Must be between ${fieldRules.min} and ${fieldRules.max}`;
                        } else if (fieldRules.min !== undefined) {
                            message = this.errorMessages.min(fieldRules.min);
                        } else if (fieldRules.max !== undefined) {
                            message = this.errorMessages.max(fieldRules.max);
                        }
                        this.showFieldError(field, message);
                        return;
                    }
                }

                if (fieldRules.date && value && !this.validateDate(value)) {
                    this.showFieldError(field, this.errorMessages.date);
                    return;
                }

                if (fieldRules.time && value && !this.validateTime(value)) {
                    this.showFieldError(field, this.errorMessages.time);
                    return;
                }

                if (fieldRules.futureDate && value) {
                    const date = new Date(value);
                    if (date <= new Date()) {
                        this.showFieldError(field, this.errorMessages.futureDate);
                        return;
                    }
                }

                if (fieldRules.pastDate && value) {
                    const date = new Date(value);
                    if (date >= new Date()) {
                        this.showFieldError(field, this.errorMessages.pastDate);
                        return;
                    }
                }

                if (fieldRules.minLength && value.length < fieldRules.minLength) {
                    this.showFieldError(field, this.errorMessages.minLength(fieldRules.minLength));
                    return;
                }

                if (fieldRules.maxLength && value.length > fieldRules.maxLength) {
                    this.showFieldError(field, this.errorMessages.maxLength(fieldRules.maxLength));
                    return;
                }

                if (fieldRules.custom && typeof fieldRules.custom === 'function') {
                    const customResult = fieldRules.custom(value, form);
                    if (customResult !== true) {
                        this.showFieldError(field, customResult || 'Invalid value');
                        return;
                    }
                }

                // If we get here, validation passed
                this.showFieldSuccess(field);
            });

            // Clear validation on input
            field.addEventListener('input', () => {
                this.clearFieldValidation(field);
            });
        }
    }
}

// Error Handling Utilities
class ErrorHandler {
    constructor() {
        this.errorTypes = {
            NETWORK: 'network',
            VALIDATION: 'validation',
            AUTH: 'authentication',
            PERMISSION: 'permission',
            SERVER: 'server',
            CLIENT: 'client'
        };
    }

    // Categorize error
    categorizeError(error) {
        if (error.code === 'auth/invalid-email' || error.code === 'auth/wrong-password') {
            return this.errorTypes.AUTH;
        }
        if (error.code === 'permission-denied') {
            return this.errorTypes.PERMISSION;
        }
        if (error.message && error.message.includes('network')) {
            return this.errorTypes.NETWORK;
        }
        if (error.message && error.message.includes('validation')) {
            return this.errorTypes.VALIDATION;
        }
        if (error.code && error.code.startsWith('auth/')) {
            return this.errorTypes.AUTH;
        }
        return this.errorTypes.CLIENT;
    }

    // Get user-friendly error message
    getUserFriendlyMessage(error, context = '') {
        const category = this.categorizeError(error);
        
        switch (category) {
            case this.errorTypes.NETWORK:
                return 'Network error. Please check your internet connection and try again.';
            case this.errorTypes.AUTH:
                if (error.code === 'auth/invalid-email') {
                    return 'Invalid email address. Please check and try again.';
                }
                if (error.code === 'auth/wrong-password') {
                    return 'Incorrect password. Please try again.';
                }
                if (error.code === 'auth/user-not-found') {
                    return 'No account found with this email address.';
                }
                return 'Authentication error. Please check your credentials and try again.';
            case this.errorTypes.PERMISSION:
                return 'You do not have permission to perform this action.';
            case this.errorTypes.VALIDATION:
                return error.message || 'Please check your input and try again.';
            case this.errorTypes.SERVER:
                return 'Server error. Please try again later.';
            default:
                return error.message || 'An unexpected error occurred. Please try again.';
        }
    }

    // Handle error with logging
    handleError(error, context = '', showToUser = true) {
        console.error(`Error in ${context}:`, error);
        
        const userMessage = this.getUserFriendlyMessage(error, context);
        
        if (showToUser) {
            this.showError(userMessage);
        }
        
        return {
            category: this.categorizeError(error),
            message: userMessage,
            originalError: error
        };
    }

    // Show error message to user
    showError(message, title = 'Error') {
        // Try to use existing error display method
        const errorElement = document.getElementById('errorMessage') || 
                           document.querySelector('.error-message') ||
                           document.querySelector('.alert-error');
        
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = 'block';
            errorElement.className = 'error-message';
            
            // Auto-hide after 5 seconds
            setTimeout(() => {
                errorElement.style.display = 'none';
            }, 5000);
        } else {
            // Fallback to alert
            alert(`${title}: ${message}`);
        }
    }

    // Show success message
    showSuccess(message, title = 'Success') {
        const successElement = document.getElementById('successMessage') || 
                              document.querySelector('.success-message') ||
                              document.querySelector('.alert-success');
        
        if (successElement) {
            successElement.textContent = message;
            successElement.style.display = 'block';
            successElement.className = 'success-message';
            
            // Auto-hide after 3 seconds
            setTimeout(() => {
                successElement.style.display = 'none';
            }, 3000);
        } else {
            // Fallback to alert
            alert(`${title}: ${message}`);
        }
    }

    // Show loading state
    showLoading(element, text = 'Loading...') {
        if (element) {
            element.disabled = true;
            element.dataset.originalText = element.textContent;
            element.textContent = text;
        }
    }

    // Hide loading state
    hideLoading(element) {
        if (element && element.dataset.originalText) {
            element.disabled = false;
            element.textContent = element.dataset.originalText;
            delete element.dataset.originalText;
        }
    }

    // Wrap async function with error handling
    async wrapAsync(fn, context = '', showToUser = true) {
        try {
            return await fn();
        } catch (error) {
            return this.handleError(error, context, showToUser);
        }
    }
}

// Global instances
window.validationUtils = new ValidationUtils();
window.errorHandler = new ErrorHandler();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ValidationUtils, ErrorHandler };
}
