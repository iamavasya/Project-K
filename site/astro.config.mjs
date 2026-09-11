// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// Сайт Лілейки: візитка + довідка. Довідка не пишеться тут — вона живе в `docs/user/` у корені
// репозиторію і копіюється сюди скриптом `scripts/sync-docs.mjs` перед dev і build, тож одна
// правда і для сайту, і для тих, хто читає markdown на GitHub.
export default defineConfig({
	integrations: [
		starlight({
			title: 'Лілейка',
			description: 'Система для куреня: реєстр, проби й вмілості, календар і планування в одному місці.',
			defaultLocale: 'root',
			locales: {
				root: { label: 'Українська', lang: 'uk' },
			},
			logo: {
				light: './src/assets/lileyka-mark.svg',
				dark: './src/assets/lileyka-mark-dark.svg',
				alt: 'Лілейка',
			},
			favicon: '/favicon.svg',
			customCss: ['./src/styles/brand.css'],
			social: [
				{ icon: 'github', label: 'GitHub', href: 'https://github.com/iamavasya/Project-K' },
			],
			sidebar: [
				{
					label: 'Для користувача',
					items: [
						{ label: 'Початок', items: [{ autogenerate: { directory: 'user/start' } }] },
						{ label: 'За роллю', items: [{ autogenerate: { directory: 'user/roles' } }] },
						{ label: 'Функції', items: [{ autogenerate: { directory: 'user/features' } }] },
						{ slug: 'user/account' },
						{ slug: 'user/admin' },
						{ slug: 'user/self-host' },
						{ slug: 'user/about' },
					],
				},
				{
					label: 'Для розробника',
					items: [
						{ label: 'Основи', items: [{ autogenerate: { directory: 'dev/core' } }] },
						{ label: 'Гайди', items: [{ autogenerate: { directory: 'dev/guides' } }] },
						{ label: 'Експлуатація', items: [{ autogenerate: { directory: 'dev/operations' } }] },
						{ label: 'Self-host', items: [{ autogenerate: { directory: 'dev/self-host' } }] },
						{ label: 'DevLog', collapsed: true, items: [{ autogenerate: { directory: 'dev/devlog' } }] },
					],
				},
			],
			credits: false,
			head: [
				{
					tag: 'link',
					attrs: {
						rel: 'preload',
						href: '/fonts/manrope-var-cyrillic.woff2',
						as: 'font',
						type: 'font/woff2',
						crossorigin: 'anonymous',
					},
				},
			],
		}),
	],
});
