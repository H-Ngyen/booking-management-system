'use client';

import * as React from 'react';
import * as LabelPrimitive from '@radix-ui/react-label';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/cn';

const Label = React.forwardRef<
  React.ElementRef<typeof LabelPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof LabelPrimitive.Root>
>(({ className, ...props }, ref) => (
  <LabelPrimitive.Root
    ref={ref}
    className={cn('mb-1.5 block text-sm font-semibold text-ink', className)}
    {...props}
  />
));
Label.displayName = LabelPrimitive.Root.displayName;

const labelVariants = cva('mb-1.5 block text-sm font-semibold text-ink');

export { Label, labelVariants };
export type { VariantProps };
