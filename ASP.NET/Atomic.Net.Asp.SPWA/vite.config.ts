import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

const __BASE_URL__ = 
    process.env.ProxiedUrl ??
    process.env.services__AIPRACTICEWEBAPI__https__0 ?? 
    '';

console.log('Proxy target:', __BASE_URL__);

// https://vite.dev/config/
export default defineConfig({
    plugins: [svelte()],
    define: {
        __BASE_URL__: JSON.stringify(__BASE_URL__)
    },
    server: {
        host: '0.0.0.0',
        port: parseInt(process.env.PORT ?? "5173"),
        // proxy: {
        //     '/api': proxyTarget
        // },
        watch: {
            ignored: ['**/node_modules/**']
        },
    }
})
