# MathTestAssessment

MathTestAssessment is a .NET application for importing, automatically grading,
and analyzing arithmetic exams. Teachers upload XML documents containing exams for
one or multiple students. Processing runs asynchronously, mathematical expressions
are evaluated automatically, and Teachers and Students receive role-specific results.

## 1. System setup

### Requirements

- .NET 10 SDK
- SQL Server
- Visual Studio with .NET 10 support

SQL Server must be running, and the configured identity must have permission to
create and update the database. With `Trusted_Connection=True`, this is the Windows
account running the API.

### Configuration

In Visual Studio, right-click `MathTestAssessment.Api`, select **Manage User Secrets**,
and add:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MathTestAssessment;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_SECURE_RANDOM_KEY_WITH_AT_LEAST_32_BYTES",
    "Issuer": "MathTestAssessment.Api",
    "Audience": "MathTestAssessment.Clients"
  },
  "SeedAdmin": {
    "Email": "admin@mathtestassessment.local",
    "Password": "Your_Strong_Password"
  }
}
```

Replace the server, JWT key, and password with environment-specific values. The
Admin password must contain at least eight characters, one lowercase letter, and
one digit.

Blazor's API address is configured under `Api:BaseUrl` in
`MathTestAssessment.Web/appsettings.Development.json`. The HTTPS launch profile
also sets `Api__BaseUrl` in `Properties/launchSettings.json`, which takes precedence.
Keep both addresses consistent if the API port changes. Accept the ASP.NET Core
development certificate prompt in Visual Studio when running locally.

Background-processing settings, including retry count, parallel jobs, polling,
heartbeat, and lease duration, are defined in
`MathTestAssessment.Api/appsettings.json`.

### Starting the system

Open `MathTestAssessment.slnx`, restore NuGet packages, select the **API and Blazor**
multiple-project launch profile, and run it with the HTTPS profiles. If Visual Studio
does not show this profile, configure both host projects as startup projects.

On startup, the API automatically:

- Creates or updates the database.
- Applies migrations for all four DbContexts.
- Creates the Admin, Teacher, and Student roles.
- Creates the configured Admin when it does not exist.
- Starts the background processor.

Repeated startup is safe, and an existing Admin password is not overwritten.

- Web application: <https://localhost:7289>
- API: <https://localhost:7116>
- Swagger: <https://localhost:7116/swagger>

The setup is complete when the API starts without migration errors, the Web
application opens, and the configured Admin can log in.

## 2. Overview, architecture, and integration

### Main workflow

1. Admin creates a Teacher account; the Teacher changes its password at first login.
2. Teacher creates student records with unique IDs.
3. Admin creates Student login accounts by selecting those existing records.
4. Teacher uploads XML; the API creates a persistent background job.
5. The job validates the XML and uses the Math Engine to grade every task.
6. Teacher reviews the results; Students sign in, change their initial passwords,
   and view their own exams.

Exams can only be created through XML import.

### Architecture

The system follows a **Clean Modular Monolith** approach and contains five business
modules:

- **Identity** — accounts, roles, authentication, and passwords.
- **Students** — student records and IDs.
- **Imports** — XML validation and import coordination.
- **Examinations** — exams, tasks, grading, and results.
- **Analytics** — Teacher and Student analytical views.

`Shared/MathTestAssessment.BackgroundJobs` provides persistent job state, retries,
leases, and recovery. `MathTestAssessment.MathEngine` is an independent arithmetic
processor.

`MathTestAssessment.Api` is the application and integration boundary.
`MathTestAssessment.Web` is the Blazor and Radzen client. The Web application
communicates with the API and does not access the database directly.

This architecture provides clear separation of responsibilities without introducing
the operational complexity of microservices.

### External integration

Third-party applications integrate through the ASP.NET Core API and do not require
the Blazor UI or direct database access.

The integration flow is:

1. `POST /api/auth/login` with a Teacher email and password to receive a JWT.
2. If `mustChangePassword` is true, call `POST /api/auth/change-password` with
   `currentPassword` and `newPassword`, then use the replacement JWT returned.
3. Send the JWT as `Authorization: Bearer <token>` and upload the XML through
   `POST /api/import-jobs` using multipart form field `file`.
4. HTTP `202 Accepted` returns a job ID; it confirms receipt, not successful grading.
   Poll `GET /api/import-jobs/{id}` until `Completed` or `Failed`.
5. Read `GET /api/import-jobs/{id}/summary`, `GET /api/exams/paged`, and
   `GET /api/exams/{id}` for the results. A failed job exposes its `errorMessage`.

The integration uses HTTP, XML, JSON, JWT bearer authentication, and standard HTTP
status codes. Swagger documents and demonstrates the available endpoints. Internal
modules can change without changing this external integration flow.

## 3. Roles and core functionality

**Admin** manages accounts and Failed background jobs. Admin can create Teacher
accounts, create Student accounts from existing student records, reset passwords,
view jobs, and restart Failed jobs. Admin cannot upload exams, view academic results,
or delete students.

**Teacher** manages students and exam imports. Teacher can create and update student
records, upload XML, and review imported exams and analytics. Teacher cannot create
exams manually, delete students, or view another Teacher's exams.

**Student** can view only their own exams, scores, task results, and analytics.
Students cannot upload XML or access another student's information.

A student record is created by a Teacher. Admin creates the Student login account by
selecting a student record without an existing account. Teacher and Student IDs
must be unique across both roles, and email addresses must be unique across all
accounts. A database-scoped lock serializes ID validation and saving across
Teacher registration and Student creation or updates, including concurrent API instances.

New Teacher and Student accounts receive temporary passwords. The password must be
changed during the first login. Admin password reset invalidates existing sessions
and requires another password change.

## 4. XML import and analytics

The XML follows the structure supplied in the case-study requirement:

```text
Teacher → Students → Student → Exam → Task
```

`Teacher ID` and `Student ID` are the external identifiers entered as ID in the UI.
`Exam Id` identifies an exam for that Teacher and Student. `Task id` identifies a
task inside the exam.

For example, a Teacher with ID `11111` and a Student with ID `12345` use
`<Teacher ID="11111">` and `<Student ID="12345">` in the XML. Enter the same IDs
when creating their records in the UI. These are external business identifiers, not the
internal database `Guid` IDs. Teacher ID is entered when Admin creates the Teacher
account; Student ID is entered when Teacher creates the student record.

The Teacher ID must match the authenticated Teacher. Every Student ID in the XML
must already exist in the database. XML import does not create students.

If a Student ID is missing, the complete import fails and no exams are stored. An
Exam ID must be unique for the same Teacher and Student, but the same ID can be used
for different students.

The import is transactional. Invalid XML, missing students, duplicate exams, or
malformed task data reject the complete file. A task whose expression cannot be
evaluated, such as division by zero, is stored as unevaluated with its error code;
it does not reject otherwise valid exam data.

After upload, the Import Details page checks the job status every two seconds and
displays either the import summary or the processing error.

The XML Imports page lists the signed-in Teacher's import history with server-side
pagination and status updates every 20 seconds. Teachers open details directly
from this list without entering a job ID. New uploads retain the original filename.
Student creation and editing are available on dedicated pages from the Students list.

Sample files are available in `MathTestAssessment.Web/Samples`:

- [single-exam.xml](MathTestAssessment.Web/Samples/single-exam.xml) — one student and one exam.
- [batch-exams.xml](MathTestAssessment.Web/Samples/batch-exams.xml) — two students and four exams.
- [five-exams.xml](MathTestAssessment.Web/Samples/five-exams.xml) — one student, five exams, and 25 tasks.

The samples use Teacher ID `11111` and Student IDs `12345` and `54321`. These
IDs must exist before upload.

Exam list endpoints use `page` and `pageSize` (default 10, maximum 100) and return
`items` with `totalCount`. Use `/api/exams/paged` or
`/api/exams/student/{studentId}/paged`; there are no unbounded exam list endpoints.

Analytics show exams, scores, correct and incorrect tasks, submitted answers,
expected answers, and evaluation errors. Teacher views are limited to their imported
exams, while Students see only their own data. Scores are correct answers divided by
evaluated tasks; unevaluated tasks are excluded.

## 5. Technical decisions and unit tests

### Math Engine

`MathEngine` is independent from the API, UI, Identity, and database. It supports
arithmetic operators, parentheses, decimal values, unary values, and standard
precedence.

```text
Expression → Tokenizer → Parser → Result
```

A custom parser provides controlled grammar, deterministic errors, complexity limits,
and protection from arbitrary code execution. `DataTable.Compute` provides less
control, Roslyn introduces unnecessary security risk, and a third-party library is
unnecessary for the required arithmetic scope.

### Background processing

XML processing is asynchronous so uploads do not remain open during grading. Jobs
are persisted in SQL Server and survive application restarts.

`BackgroundJobs` is Shared infrastructure because it manages technical concerns such
as job state, claims, leases, retries, and recovery. The Imports module contains the
exam-specific XML logic.

Persistent jobs were selected instead of an in-memory queue to avoid losing pending
work. Hangfire and Azure Service Bus are valid alternatives for larger distributed
systems but add unnecessary infrastructure for this case study.

### Unit tests

The `MathTestAssessment.UnitTests` project verifies independent business logic without
accessing the database.

The tests cover arithmetic evaluation, operator precedence, invalid expressions,
division by zero, overflow, complexity limits, XML validation, unsafe XML, and
correct, incorrect, or unevaluated grading results. API-client tests use a fake HTTP
handler to verify readable validation messages without network calls.

Theory tests cover multiple inputs with the same behavior, while Fact tests cover
individual parameterless cases.

Run the tests through Visual Studio's Test Explorer by selecting **Run All Tests**.
They require no database records or running hosts.

The XML identifier is stored in the `ID` column of `Students` and `AspNetUsers`.
Their internal Guid primary keys are stored in `InternalId`; API DTOs retain
`ExternalStudentId` and `ExternalTeacherId` to distinguish the two identifiers.
