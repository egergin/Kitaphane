import { type RefObject, useEffect } from 'react'

type InfiniteScrollOptions = {
  hasNextPage: boolean
  isFetchingNextPage: boolean
  fetchNextPage: () => unknown
}

/** Calls fetchNextPage when the sentinel element gets close to the viewport. */
export function useInfiniteScroll(
  sentinelRef: RefObject<HTMLElement | null>,
  { hasNextPage, isFetchingNextPage, fetchNextPage }: InfiniteScrollOptions,
) {
  useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel || !hasNextPage) return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting && !isFetchingNextPage) void fetchNextPage()
      },
      { rootMargin: '600px 0px' },
    )
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [sentinelRef, hasNextPage, isFetchingNextPage, fetchNextPage])
}
