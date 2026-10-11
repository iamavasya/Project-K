import { KurinScopeOption } from '../../auth/auth.models';
import {
  displayCodeName,
  fitWithin,
  groupRole,
  healthUrl,
  isPrivileged,
  kurinLabel,
  passwordMeetsRules,
  sortKurins,
  versionLabel,
} from './account.labels';
import { pingResult } from './cold-start';
import { HttpErrorResponse } from '@angular/common/http';

const option = (kurinKey: string, kurinNumber: number, namedAfter: string | null = null): KurinScopeOption => ({
  kurinKey,
  kurinNumber,
  namedAfter,
  branch: 'UPYu',
  kind: 'Youth',
});

describe('account labels', () => {
  it('names a kurin as the web tile does', () => {
    expect(kurinLabel(option('a', 7))).toBe('к. ч. 7');
    expect(kurinLabel(option('b', 42, 'Сірого Лева'))).toBe('к. ч. 42 ім. Сірого Лева');
  });

  it('puts the kurin acted in first, then by number', () => {
    const sorted = sortKurins([option('c', 50), option('b', 42), option('a', 7)], 'b');
    expect(sorted.map((o) => o.kurinNumber)).toEqual([42, 7, 50]);
  });

  it('words a гурток role', () => {
    expect(groupRole({ isOwn: true, isLed: true })).toBe('мій гурток · провід');
    expect(groupRole({ isOwn: true, isLed: false })).toBe('мій гурток');
    expect(groupRole({ isOwn: false, isLed: true })).toBe('впорядник');
  });

  it('treats admins and whole-kurin managers as privileged, like the web', () => {
    expect(isPrivileged({ isAdmin: true, permissions: [] })).toBe(true);
    expect(isPrivileged({ isAdmin: false, permissions: ['Group:Manage:KurinWide'] })).toBe(true);
    expect(isPrivileged({ isAdmin: false, permissions: ['Group:Update:Own'] })).toBe(false);
    expect(isPrivileged(null)).toBe(false);
  });

  it('checks the web’s password rules', () => {
    expect(passwordMeetsRules('Secret1!')).toBe(true);
    expect(passwordMeetsRules('secret1!')).toBe(false);
    expect(passwordMeetsRules('Secret!!')).toBe(false);
    expect(passwordMeetsRules('Sec1!')).toBe(false);
  });

  it('hides placeholder code names', () => {
    expect(displayCodeName('LocalDevelopment')).toBeNull();
    expect(displayCodeName('  ')).toBeNull();
    expect(versionLabel('v1.0.0', 'Ant')).toBe('v1.0.0 «Ant»');
    expect(versionLabel('v0.0.0-dev', 'LocalDevelopment')).toBe('v0.0.0-dev');
  });

  it('finds /health beside /api', () => {
    expect(healthUrl('https://api.example.test/api')).toBe('https://api.example.test/health');
    expect(healthUrl('https://api.example.test/api/')).toBe('https://api.example.test/health');
    expect(healthUrl('https://x.test/other')).toBe('https://x.test/other/health');
  });

  it('fits a photo within the longer side and never enlarges', () => {
    expect(fitWithin(4032, 3024, 1920)).toEqual({ width: 1920, height: 1440 });
    expect(fitWithin(1170, 2532, 1920)).toEqual({ width: 887, height: 1920 });
    expect(fitWithin(800, 600, 1920)).toEqual({ width: 800, height: 600 });
  });

  it('reads a server that is waking up apart from one out of reach', () => {
    expect(pingResult(null)).toBe('up');
    expect(pingResult(new HttpErrorResponse({ status: 503 }))).toBe('waking');
    expect(pingResult(new Error('Timeout'))).toBe('waking');
    expect(pingResult(new HttpErrorResponse({ status: 0 }))).toBe('unreachable');
    expect(pingResult(new HttpErrorResponse({ status: 404 }))).toBe('up');
  });
});
