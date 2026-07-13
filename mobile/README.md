# IBS Mobile

تطبيق مشتركين IBS — Vue 3 + Capacitor + RTL

## التشغيل

```bash
cd mobile
npm install
npm run dev
```

تأكد أن الـ Backend يعمل على العنوان الموجود في `.env`:

```
VITE_API_BASE_URL=http://localhost:5026
```

## بناء Capacitor (Android)

```bash
npm run build
npx cap add android
npx cap sync
npx cap open android
```

## الشاشات

- تسجيل الدخول (`/api/Auth/login`)
- الرئيسية — معلومات مالية واشتراك
- الفواتير — `/api/Subscriber/invoices`
- المقبوضات — `/api/Subscriber/receivables`
- التجديد — `/api/Subscriber/refill`
- الدفع — `/api/Subscriber/payment` + `payback`
