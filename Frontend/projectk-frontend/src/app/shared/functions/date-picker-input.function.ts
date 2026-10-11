import { DatePicker } from '@openng/optimus-ui/datepicker';

interface DatePickerInternals {
  isKeydown: boolean;
  onUserInput(event: Event): void;
}

/**
 * The date picker reads typed text only when a keydown came right before the input event (an old
 * IE placeholder workaround). Phone keyboards often skip that keydown: a suggestion tap, autocorrect,
 * IME composition, dictation or paste all change the field without one. The date then shows in the
 * field but never reaches the model, so the form saves without it. Every input event is the user's,
 * so let each one through.
 */
export function acceptDatePickerInputWithoutKeydown(): void {
  const prototype = DatePicker.prototype as unknown as DatePickerInternals;
  const onUserInput = prototype.onUserInput;
  prototype.onUserInput = function (this: DatePickerInternals, event: Event): void {
    this.isKeydown = true;
    onUserInput.call(this, event);
  };
}
