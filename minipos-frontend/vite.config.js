import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

/**
 * Vite config for Mini POS Frontend
 *
 * Dev proxy: routes API calls to the correct backend microservice.
 * Production: nginx (see nginx.conf) handles routing inside Docker.
 *
 * Backend repo: https://github.com/YOUR_ORG/minipos-backend
 * Backend URLs (local dev):
 *   POS.WebHost (SignalR + Auth) → http://localhost:5000
 *   Articles.Service             → http://localhost:5002
 *   Basket.Service (read only)   → http://localhost:5001
 *   Payment.Service (history)    → http://localhost:5003
 *   Forecourt.Service            → http://localhost:5004
 */
export default defineConfig({
  plugins: [react()],

  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },

  server: {
    port: 5173,
    proxy: {
      // SignalR WebSocket — MUST include ws:true
      '/hubs': {
        target:       'http://localhost:5000',
        ws:           true,
        changeOrigin: true
      },
      // JWT auth endpoint
      '/api/auth': {
        target:       'http://localhost:5000',
        changeOrigin: true
      },
      // Product catalogue
      '/api/v1/articles': {
        target:       'http://localhost:5002',
        changeOrigin: true
      },
      // Forecourt initial load
      '/api/forecourt': {
        target:       'http://localhost:5004',
        changeOrigin: true
      },
      // Payment transaction history (debug)
      '/api/payment': {
        target:       'http://localhost:5003',
        changeOrigin: true
      },
      // Basket debug read
      '/api/basket': {
        target:       'http://localhost:5001',
        changeOrigin: true
      }
    }
  },

  build: {
    outDir:     'dist',
    sourcemap:  false,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          signalr: ['@microsoft/signalr']
        }
      }
    }
  }
});
