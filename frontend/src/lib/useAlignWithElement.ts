import { type RefObject, useLayoutEffect, useState } from 'react'

type Anchor = 'left' | 'center'

/**
 * Distance in px from the right edge of `containerRef` to the left edge or horizontal center of the
 * first element matching `targetSelector` (e.g. a header item). Re-measured when the window or either
 * element resizes, so content can stay aligned with header items whose position depends on text width.
 * Returns null until measured or when the target is not found.
 */
export function useOffsetFromRight(
  containerRef: RefObject<HTMLElement | null>,
  targetSelector: string,
  anchor: Anchor = 'center',
) {
  const [offset, setOffset] = useState<number | null>(null)

  useLayoutEffect(() => {
    const container = containerRef.current
    const target = document.querySelector<HTMLElement>(targetSelector)
    if (!container || !target) return

    const measure = () => {
      const containerRect = container.getBoundingClientRect()
      const targetRect = target.getBoundingClientRect()
      const targetX = anchor === 'left' ? targetRect.left : targetRect.left + targetRect.width / 2
      setOffset(containerRect.right - targetX)
    }

    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(container)
    observer.observe(target)
    window.addEventListener('resize', measure)
    return () => {
      observer.disconnect()
      window.removeEventListener('resize', measure)
    }
  })

  return offset
}
