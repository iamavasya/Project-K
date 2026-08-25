// @ts-check
const eslint = require("@eslint/js");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");
const sonarjs = require("eslint-plugin-sonarjs");

// Findings report, they do not block: the analyzer sweep in 0.19.0 starts from a backlog, and the
// count is tracked in docs/quality-baseline.md rather than gating every change. Rules that are
// already clean stay at "error" so they cannot regress.
module.exports = tseslint.config(
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
      sonarjs.configs.recommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      "@angular-eslint/directive-selector": [
        "error",
        {
          type: "attribute",
          prefix: "app",
          style: "camelCase",
        },
      ],
      "@angular-eslint/component-selector": [
        "error",
        {
          type: "element",
          prefix: "app",
          style: "kebab-case",
        },
      ],
      // The Angular 22 migration stamped ChangeDetectionStrategy.Eager onto every
      // component to preserve v21 behaviour, which this rule flags by definition.
      // Keep it off until components are actually moved to OnPush.
      "@angular-eslint/prefer-on-push-component-change-detection": "off",
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          argsIgnorePattern: "^_",
          varsIgnorePattern: "^_",
          caughtErrorsIgnorePattern: "^_",
        },
      ],
    },
  },
  {
    // Sonar findings are advisory: they surface in the lint output and in CI, but a backlog from
    // first enabling them must not fail every build. Downgraded as a set so new rules arriving with
    // a plugin update inherit the same treatment.
    files: ["**/*.ts"],
    rules: Object.fromEntries(
      Object.entries(sonarjs.configs.recommended.rules ?? {})
        .filter(([name, level]) => {
          if (!name.startsWith("sonarjs/")) return false;
          // recommended lists every rule and switches the opinionated ones off — keep those off.
          const severity = Array.isArray(level) ? level[0] : level;
          return severity === "error" || severity === 2;
        })
        .map(([name]) => [name, "warn"])
    ),
  },
  {
    files: ["**/*.spec.ts", "e2e/**/*.ts"],
    rules: {
      // Test files repeat setup and fixture literals by nature, and seeded credentials are the
      // point of the fixtures — these drown out findings in application code.
      "sonarjs/no-duplicate-string": "off",
      "sonarjs/no-identical-functions": "off",
      "sonarjs/no-hardcoded-passwords": "off",
      "sonarjs/prefer-specific-assertions": "off",
    },
  },
  {
    files: ["**/*.html"],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {},
  }
);
