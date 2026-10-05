using FluentValidation;
using Hospital.Application.Auth;

namespace Hospital.Application.Auth.Validators;

public static class PasswordPolicy
{
    public static IRuleBuilderOptionsConditions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        PasswordPolicyOptions policy)
    {
        return ruleBuilder.Custom((password, context) =>
        {
            foreach (var error in PasswordRules.Evaluate(password, policy))
            {
                context.AddFailure(error);
            }
        });
    }
}
