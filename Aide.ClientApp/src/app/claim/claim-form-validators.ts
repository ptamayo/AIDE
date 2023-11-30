import { FormGroup, ValidationErrors } from "@angular/forms";

export class ClaimFormValidators {
    static AtLeastOneClaimIdentifierMustBeProvided(formGroup: FormGroup): ValidationErrors | null {
        let oneFieldIsProvided = false;
        Object.keys(formGroup.controls).forEach(key => {
            const control = formGroup.controls[key];
            if (control.value) {
                oneFieldIsProvided = true;
                return;
            }
        });
        if (!oneFieldIsProvided) {
            return { AtLeastOneClaimIdentifierMustBeProvided: true }
        }
        return null;
    }
}
