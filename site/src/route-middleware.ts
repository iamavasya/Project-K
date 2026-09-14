import { defineRouteMiddleware } from '@astrojs/starlight/route-data';
import type { SidebarEntry } from '@astrojs/starlight/utils/routing/types';

// Two audiences share one site. «Для користувача» is what the sidebar and the home page offer;
// «Для розробника» lives under /dev/ and is reached by the address alone: on those pages the
// sidebar shows only the developer tree, everywhere else only the user tree, and prev/next never
// step across the border.
const isDevPath = (path: string) => path.startsWith('/dev/') || path === '/dev';

function firstHref(entry: SidebarEntry): string | undefined {
	if (entry.type === 'link') {
		return entry.href;
	}
	for (const child of entry.entries) {
		const href = firstHref(child);
		if (href) {
			return href;
		}
	}
	return undefined;
}

export const onRequest = defineRouteMiddleware((context) => {
	const route = context.locals.starlightRoute;
	const onDevPage = isDevPath(context.url.pathname);

	route.sidebar = route.sidebar.filter((entry) => {
		const href = firstHref(entry);
		return href === undefined || isDevPath(href) === onDevPage;
	});

	const { prev, next } = route.pagination;
	route.pagination = {
		prev: prev && isDevPath(prev.href) === onDevPage ? prev : undefined,
		next: next && isDevPath(next.href) === onDevPage ? next : undefined,
	};
});
