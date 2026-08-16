using System.Collections.Generic;
using NAS.Core.Models;

namespace NAS.Core.Events
{
    // ---- Auth flow -------------------------------------------------------

    /// <summary>Raised by LoginCardController when the user taps "Log in".</summary>
    public readonly struct LoginRequestedEvent
    {
        public readonly string Email;
        public readonly string Password;
        public LoginRequestedEvent(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    /// <summary>Raised by RegisterCardController when the user taps "Register".</summary>
    public readonly struct RegisterRequestedEvent
    {
        public readonly string FirstName;
        public readonly string LastName;
        public readonly string CellNumber;
        public readonly string Email;
        public readonly string Password;
        public readonly string ConfirmPassword;

        public RegisterRequestedEvent(string firstName, string lastName, string cellNumber, string email, string password, string confirmPassword)
        {
            FirstName = firstName;
            LastName = lastName;
            CellNumber = cellNumber;
            Email = email;
            Password = password;
            ConfirmPassword = confirmPassword;
        }
    }

    /// <summary>Raised by AuthController once login or registration succeeds.</summary>
    public readonly struct AuthSucceededEvent
    {
        public readonly User User;
        public readonly string AccessToken;
        public AuthSucceededEvent(User user, string accessToken)
        {
            User = user;
            AccessToken = accessToken;
        }
    }

    /// <summary>Raised by AuthController when login or registration fails validation/the backend.</summary>
    public readonly struct AuthFailedEvent
    {
        /// <summary>General/non-field-specific error (e.g. from the backend: wrong credentials, network error).</summary>
        public readonly string Reason;
        /// <summary>Per-field validation errors (e.g. "email" -> "Enter a valid email address."). Null for a general/backend failure instead.</summary>
        public readonly IReadOnlyDictionary<string, string> FieldErrors;

        public AuthFailedEvent(string reason, IReadOnlyDictionary<string, string> fieldErrors = null)
        {
            Reason = reason;
            FieldErrors = fieldErrors;
        }
    }

    // ---- Navigation --------------------------------------------------
    // Cards raise these when they want to move to another screen but shouldn't
    // know that ParentPageController (or anything else) is the thing listening.

    public readonly struct NavigateToRegisterRequestedEvent { }
    public readonly struct NavigateToLoginRequestedEvent { }

    // ---- Car selection flow -------------------------------------------------------

    /// <summary>Raised by CarSelectionScreenController when the user picks a car and starts AR.</summary>
    public readonly struct CarSelectedEvent
    {
        public readonly VehicleInfo Vehicle;
        public CarSelectedEvent(VehicleInfo vehicle) => Vehicle = vehicle;
    }

    /// <summary>Raised by AR flow controllers when the user backs out to the estimator card.</summary>
    public readonly struct ReturnToEstimatorRequestedEvent { }

    // ---- Estimator flow -------------------------------------------------------

    /// <summary>Raised by EstimatorCardController when the user sends the estimate to the dealer.</summary>
    public readonly struct EstimateSubmittedEvent
    {
        public readonly VehicleInfo Vehicle;
        public readonly float FinancedAmount;
        public readonly float MonthlyPayment;
        public EstimateSubmittedEvent(VehicleInfo vehicle, float financedAmount, float monthlyPayment)
        {
            Vehicle = vehicle;
            FinancedAmount = financedAmount;
            MonthlyPayment = monthlyPayment;
        }
    }
}
