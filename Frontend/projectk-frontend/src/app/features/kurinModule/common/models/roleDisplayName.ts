import { LeadershipRole } from "./enums/leadership-role.enum"

export const ROLE_DISPLAY_NAMES: Record<LeadershipRole, string> = {
    [LeadershipRole.Kurinnuy]:   'Курінний',
    [LeadershipRole.Hurtkoviy]:  'Гуртковий',
    [LeadershipRole.Suddya]:     'Суддя',
    [LeadershipRole.Pysar]:      'Писар',
    [LeadershipRole.Skarbnyk]:   'Скарбник',
    [LeadershipRole.Horunjiy]:   'Хорунжий',
    [LeadershipRole.Gospodar]:   'Господар',
    [LeadershipRole.Hronikar]:   'Хронікар',
    [LeadershipRole.Instruktor]: 'Інструктор',
    [LeadershipRole.Zvyazkovyi]: 'Зв\'язковий',
    [LeadershipRole.Vykhovnyk]: 'Впорядник',
};