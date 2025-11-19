export default [
  {
    files: ["Backend/wwwroot/**/*.js"],
    languageOptions: {
      ecmaVersion: 2021,
      sourceType: "script",
      globals: {
        API_URL: "readonly",
        state: "writable",
        elements: "readonly",
        document: "readonly",
        window: "readonly",
        console: "readonly",
        alert: "readonly",
        fetch: "readonly",
        Image: "readonly",
        FormData: "readonly",
        Blob: "readonly",
        URL: "readonly",
        location: "readonly",
        localStorage: "readonly",
        navigator: "readonly",
        setTimeout: "readonly",
        setInterval: "readonly",
        clearInterval: "readonly",
        clearTimeout: "readonly",
        atob: "readonly",
        btoa: "readonly",
        FileReader: "readonly",
        File: "readonly",
        TextDecoder: "readonly",
        confirm: "readonly",
        prompt: "readonly",
        logout: "readonly"
      }
    },
    rules: {
      "no-alert": "warn",
      "no-undef": "error",
      "no-unused-vars": "warn",
      "prefer-const": "warn",
      "no-console": ["warn", { allow: ["error", "warn", "info"] }]
    }
  }
];
