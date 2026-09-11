// Збирає довідку для Starlight з одного джерела правди — репозиторію — у `src/content/docs/`.
//
// Дві гілки:
//  - «Для користувача»: `docs/user/**` копіюється як є (уже у форматі Starlight).
//  - «Для розробника»: `docs/dev/**` копіюється як є, а кореневі документи репозиторію
//    (ARCHITECTURE, CONTRIBUTING, BRANDBOOK, SECURITY, docs/self-host …) підтягуються з
//    frontmatter, згенерованим із їхнього `# H1`, і з переписаними посиланнями між собою.
//    Так на GitHub лишається звичний markdown, а сайт не тримає копій.
//
// Тека `src/content/docs/{user,dev}` генерується і не комітиться. Запуск: перед `dev` і `build`.
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, '../..');
const content = resolve(here, '../src/content/docs');

/** Кореневі документи → сторінки розділу «Для розробника». */
const rootDocs = [
	{ from: 'ARCHITECTURE.md', to: 'dev/core/architecture.md', order: 1, description: 'Шари, модулі, шлях запиту, авторизація, середовища.' },
	{ from: 'CONTRIBUTING.md', to: 'dev/core/contributing.md', order: 2, description: 'Конвенції, за якими пишеться новий код.' },
	{ from: 'BRANDBOOK.md', to: 'dev/core/brandbook.md', order: 3, description: 'Візуальна система: кольори, шрифти, компоненти, правила.' },
	{ from: 'SECURITY.md', to: 'dev/core/security.md', order: 4, description: 'Як повідомити про вразливість і які версії підтримуються.' },
	{ from: 'docs/quality-baseline.md', to: 'dev/core/quality-baseline.md', order: 5, description: 'Тести, лінт і те, що вважається базовою лінією.' },
	{ from: 'docs/observability.md', to: 'dev/operations/observability.md', order: 1, description: 'Логи, метрики, здоровʼя сервісу в проді.' },
	{ from: 'docs/data-retention.md', to: 'dev/operations/data-retention.md', order: 2, description: 'Що система зберігає і як довго.' },
	{ from: 'docs/self-host/README.md', to: 'dev/self-host/index.md', order: 1, description: 'Поставити Лілейку на власний сервер.' },
	{ from: 'docs/self-host/backup-restore.md', to: 'dev/self-host/backup-restore.md', order: 2, description: 'Резервні копії та відновлення.' },
	{ from: 'docs/self-host/update.md', to: 'dev/self-host/update.md', order: 3, description: 'Оновлення self-host-інсталяції.' },
];

/** Посилання між кореневими документами → адреси на сайті. */
const linkMap = new Map([
	['ARCHITECTURE.md', '/dev/core/architecture/'],
	['CONTRIBUTING.md', '/dev/core/contributing/'],
	['BRANDBOOK.md', '/dev/core/brandbook/'],
	['SECURITY.md', '/dev/core/security/'],
	['docs/quality-baseline.md', '/dev/core/quality-baseline/'],
	['docs/observability.md', '/dev/operations/observability/'],
	['docs/data-retention.md', '/dev/operations/data-retention/'],
	['docs/self-host/README.md', '/dev/self-host/'],
	['docs/self-host/backup-restore.md', '/dev/self-host/backup-restore/'],
	['docs/self-host/update.md', '/dev/self-host/update/'],
	['README.md', '/dev/self-host/'],
	['backup-restore.md', '/dev/self-host/backup-restore/'],
	['update.md', '/dev/self-host/update/'],
]);

function copyTree(from, to) {
	const source = resolve(repo, from);
	if (!existsSync(source)) {
		return 0;
	}
	cpSync(source, resolve(content, to), {
		recursive: true,
		filter: (path) => !/\.(tmp|bak)$/i.test(path),
	});
	return 1;
}

function escapeYaml(text) {
	return `'${text.replace(/'/g, "''")}'`;
}

function rewriteLinks(markdown) {
	return markdown.replace(/\]\(([^)\s#]+)(#[^)]*)?\)/g, (match, target, hash = '') => {
		const key = target.replace(/^\.\//, '');
		const mapped = linkMap.get(key);
		return mapped ? `](${mapped}${hash})` : match;
	});
}

function importRootDoc({ from, to, order, description }) {
	const source = resolve(repo, from);
	if (!existsSync(source)) {
		console.warn(`sync-docs: пропущено ${from} — файла немає`);
		return 0;
	}

	let markdown = readFileSync(source, 'utf8').replace(/^﻿/, '').replace(/\r\n/g, '\n');
	let title = to.split('/').pop().replace(/\.md$/, '');
	const heading = markdown.match(/^# (.+)$/m);
	if (heading) {
		title = heading[1].trim();
		markdown = markdown.replace(heading[0], '').replace(/^\n+/, '');
	}

	const frontmatter = [
		'---',
		`title: ${escapeYaml(title)}`,
		`description: ${escapeYaml(description)}`,
		'sidebar:',
		`  order: ${order}`,
		'---',
		'',
		`:::note[Джерело]`,
		`Ця сторінка збирається з [\`${from}\`](https://github.com/iamavasya/Project-K/blob/main/${from}) у репозиторії. Правити треба там.`,
		':::',
		'',
	].join('\n');

	const target = resolve(content, to);
	mkdirSync(dirname(target), { recursive: true });
	writeFileSync(target, frontmatter + rewriteLinks(markdown));
	return 1;
}

for (const branch of ['user', 'dev']) {
	rmSync(resolve(content, branch), { recursive: true, force: true });
}

const copied = copyTree('docs/user', 'user') + copyTree('docs/dev', 'dev');
const imported = rootDocs.map(importRootDoc).reduce((sum, n) => sum + n, 0);
console.log(`sync-docs: скопійовано тек — ${copied}, зібрано кореневих документів — ${imported}`);
