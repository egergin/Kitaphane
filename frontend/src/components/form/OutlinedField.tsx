import { type ComponentProps, type ReactNode, useId } from 'react'
import { cn } from '@/lib/utils'

type OutlinedFrameProps = {
  label: string
  error?: string
  className?: string
  /** Renders the control; receives the id and ARIA props to put on it. */
  children: (control: { id: string; 'aria-invalid'?: true; 'aria-describedby'?: string }) => ReactNode
  /** Element rendered at the right end of the field (e.g. show-password button). */
  endAdornment?: ReactNode
  id?: string
}

/**
 * Field frame with the label sitting in the top border, as in the Penpot forms.
 * Uses fieldset/legend so the border is notched without background tricks.
 */
export function OutlinedFrame({ label, error, className, children, endAdornment, id }: OutlinedFrameProps) {
  const generatedId = useId()
  const controlId = id ?? generatedId
  const errorId = `${controlId}-error`

  return (
    <div className={cn('flex flex-col gap-1', className)}>
      <fieldset
        className={cn(
          'flex items-center rounded-[10px] border-2 border-input px-3 transition-colors focus-within:border-primary',
          error && 'border-destructive focus-within:border-destructive',
        )}
      >
        <legend className="px-1 text-base leading-none">
          <label htmlFor={controlId}>{label}</label>
        </legend>
        {children({
          id: controlId,
          'aria-invalid': error ? true : undefined,
          'aria-describedby': error ? errorId : undefined,
        })}
        {endAdornment}
      </fieldset>
      {error && (
        <p id={errorId} className="px-1 text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  )
}

const controlClassName = 'w-full min-w-0 bg-transparent px-1 text-base outline-none disabled:cursor-not-allowed disabled:opacity-60'
// Note: CSS treats <select> as :read-only, so the read-only look is applied to inputs and textareas only.
const readOnlyClassName = 'read-only:cursor-default read-only:text-muted-foreground'

type OutlinedFieldProps = ComponentProps<'input'> & {
  label: string
  error?: string
  endAdornment?: ReactNode
}

export function OutlinedField({ label, error, endAdornment, className, id, ...inputProps }: OutlinedFieldProps) {
  return (
    <OutlinedFrame label={label} error={error} endAdornment={endAdornment} className={className} id={id}>
      {(control) => <input {...control} className={cn(controlClassName, readOnlyClassName, 'h-9 pb-1.5')} {...inputProps} />}
    </OutlinedFrame>
  )
}

type OutlinedSelectProps = ComponentProps<'select'> & {
  label: string
  error?: string
}

export function OutlinedSelect({ label, error, className, id, children, ...selectProps }: OutlinedSelectProps) {
  return (
    <OutlinedFrame label={label} error={error} className={className} id={id}>
      {(control) => (
        <select {...control} className={cn(controlClassName, 'h-9 cursor-pointer pb-1.5')} {...selectProps}>
          {children}
        </select>
      )}
    </OutlinedFrame>
  )
}

type OutlinedTextareaProps = ComponentProps<'textarea'> & {
  label: string
  error?: string
}

export function OutlinedTextarea({ label, error, className, id, ...textareaProps }: OutlinedTextareaProps) {
  return (
    <OutlinedFrame label={label} error={error} className={className} id={id}>
      {(control) => <textarea {...control} className={cn(controlClassName, readOnlyClassName, 'min-h-28 resize-y pb-2 lg:min-h-20')} {...textareaProps} />}
    </OutlinedFrame>
  )
}
