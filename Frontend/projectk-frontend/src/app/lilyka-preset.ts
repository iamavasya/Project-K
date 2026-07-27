import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const LilykaPreset = definePreset(Aura, {
  primitive: {
    borderRadius: {
      md: '8px'
    }
  },
  semantic: {
    navigation: {
      item: {
        borderRadius: '{border.radius.md}'
      }
    },
    primary: {
      50:  '#EAF3EE',
      100: '#CFE5D9',
      200: '#A6D0BD',
      300: '#74B79C',
      400: '#3E9A78',
      500: '#14805B',
      600: '#0E6E4E',
      700: '#0B5A40',
      800: '#084733',
      900: '#063526',
      950: '#032117'
    },
    colorScheme: {
      light: {
        primary: {
          color: '{primary.600}',
          contrastColor: '#ffffff',
          hoverColor: '{primary.700}',
          activeColor: '{primary.800}'
        },

        text: {
          color: '{surface.900}',
          hoverColor: '{surface.900}',
          mutedColor: '{surface.600}',
          hoverMutedColor: '{surface.700}'
        },
        surface: {
          0: '#ffffff',
          50: '#F7F9F8',
          100: '#EFF1F0',
          200: '#E4E8E6',
          300: '#D5DAD7',
          400: '#A3ABA7',
          500: '#8E9793',
          600: '#6E7873',
          700: '#4A5350',
          800: '#2E3633',
          900: '#101413',
          950: '#0A0D0C'
        }
      },
      dark: {
        primary: {
          color: '#34C48D',
          contrastColor: '#052018',
          hoverColor: '#4FD49E',
          activeColor: '#21A874'
        },

        surface: {
          0: '#E9EDEA',
          50: '#DDE2DF',
          100: '#C7CFCB',
          200: '#B0BAB5',
          300: '#A5B0AB',
          400: '#9BA8A2',
          500: '#6C7570',
          600: '#333B37',
          700: '#232A27',
          800: '#1D2320',
          900: '#161B19',
          950: '#0D1110'
        }
      }
    }
  },

  components: {
    tag: {
      colorScheme: {
        light: {
          info: { background: '{primary.50}', color: '{primary.700}' },
          warn: { background: '#FBE9DC', color: '#A8541B' }
        },
        dark: {
          info: { background: '#1D2E27', color: '#9FE3C2' },
          warn: { background: '#2A1F16', color: '#F3B784' }
        }
      }
    },
    message: {
      colorScheme: {
        light: {
          info: {
            background: '{primary.50}',
            borderColor: '{primary.100}',
            color: '{primary.700}',
            shadow: 'none',
            closeButton: { hoverBackground: '{primary.100}', focusRing: { color: '{primary.700}', shadow: 'none' } },
            outlined: { color: '{primary.700}', borderColor: '{primary.700}' },
            simple: { color: '{primary.700}' }
          },
          warn: {
            background: '#FBE9DC',
            borderColor: '#F4D2B8',
            color: '#A8541B',
            shadow: 'none',
            closeButton: { hoverBackground: '#F4D2B8', focusRing: { color: '#A8541B', shadow: 'none' } },
            outlined: { color: '#A8541B', borderColor: '#A8541B' },
            simple: { color: '#A8541B' }
          }
        },
        dark: {
          info: {
            background: '#1D2E27',
            borderColor: '#245039',
            color: '#9FE3C2',
            shadow: 'none',
            closeButton: { hoverBackground: '#245039', focusRing: { color: '#9FE3C2', shadow: 'none' } },
            outlined: { color: '#9FE3C2', borderColor: '#9FE3C2' },
            simple: { color: '#9FE3C2' }
          },
          warn: {
            background: '#2A1F16',
            borderColor: '#3D2D1F',
            color: '#F3B784',
            shadow: 'none',
            closeButton: { hoverBackground: '#3D2D1F', focusRing: { color: '#F3B784', shadow: 'none' } },
            outlined: { color: '#F3B784', borderColor: '#F3B784' },
            simple: { color: '#F3B784' }
          }
        }
      }
    }
  }
});

