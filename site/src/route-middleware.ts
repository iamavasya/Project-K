import { defineRouteMiddleware } from '@astrojs/starlight/route-data';

// Two audiences share one sidebar: «Для користувача» open, «Для розробника» folded. What must not
// happen is prev/next walking a reader from the last user page into the developer tree, or back.
const isDevPath = (path: string) => path.startsWith('/dev/') || path === '/dev';

export const onRequest = defineRouteMiddleware((context) => {
	const route = context.locals.starlightRoute;
	const onDevPage = isDevPath(context.url.pathname);

	const { prev, next } = route.pagination;
	route.pagination = {
		prev: prev && isDevPath(prev.href) === onDevPage ? prev : undefined,
		next: next && isDevPath(next.href) === onDevPage ? next : undefined,
	};
});
