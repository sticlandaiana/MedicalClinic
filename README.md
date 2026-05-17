# 🏥 Medical Clinic — Management System

A web-based medical clinic management system built with **ASP.NET Core Razor Pages**, designed to streamline clinic operations including appointments, staff management, medical records, and patient tracking.

---

## Features

### Admin Panel
- Manage medical **specialities**, **rooms**, and **equipment**
- Add and remove **doctors** and **nurses**
- Assign **specialities to doctors**
- Manage **nurse–doctor dependencies**
- Moderate **patient reviews**
- View and filter the **audit log** (all critical actions are tracked)
- Manage **patients** and reset no-show counters

### Doctor Dashboard
- View scheduled appointments
- Write **medical record entries** (diagnosis, blood pressure, weight, temperature, notes)
- Generate **PDF medical records** for patients

### Nurse Dashboard
- View assigned appointments
- Assist doctors during consultations

### Patient Portal
- Register and log in securely
- Book, cancel, and view appointments
- View personal medical history
- Upload external documents
- Leave reviews for doctors

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core — Razor Pages |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication | ASP.NET Core Identity |
| PDF Generation | iText7 |
| Email | MailKit / MimeKit |
| Testing | xUnit + Moq + EF InMemory |

---

## Authentication & Roles

The system uses **ASP.NET Core Identity** with four roles:

- `Administrator` — full access to clinic management
- `Doctor` — access to appointments and medical records
- `Nurse` — access to assigned appointments
- `Patient` — access to personal portal

Password policy enforced: minimum 10 characters, uppercase, lowercase, digit, and special character required. Accounts are locked after **5 failed login attempts**.

---


