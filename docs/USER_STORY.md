# User Story

## Primary story

> **As a** busy professional,
> **I want to** register an account, sign in securely, and manage a personal list of tasks —
> creating them with a title, description, status and due date, then updating and deleting them —
> **so that** I can keep track of my work in one place, with the confidence that only I can see my
> tasks.

## Acceptance criteria

- **Account & access**
  - I can register with an email, display name and password.
  - Passwords must be at least 8 characters and contain a letter and a digit.
  - I can log in and stay authenticated via a token; I can log out.
  - Unauthenticated users are redirected to the sign-in page and cannot reach task data.
- **Tasks (CRUD)**
  - I can create a task with a **title** (required), optional **description**, optional **due
    date**, and a **status** (To do / In progress / Done; defaults to To do).
  - I can see a list of *my* tasks, edit any field, change status, and delete a task.
  - A due date cannot be set in the past when creating/updating.
- **Isolation & safety**
  - I can only see and modify my own tasks. Requesting another user's task returns "not found".
  - The app shows clear validation messages and loading/error states.

## Demo flow (for the presentation)

1. Open the app → redirected to a landing page → **Sign in**.
2. Click **Use demo** (`demo@taskmanager.local` / `Demo123!`) → land on **My tasks** with seeded
   sample tasks.
3. **Create** a task, **edit** its status to *In progress*, then **delete** it.
4. Register a brand-new user in another browser/profile and show that the two users never see each
   other's tasks.

## Why a task manager?

The Generative AI portion of this exercise is itself about generating a **task-management REST
API** (tasks with title, description, status, due date, linked to a user). Building the main
application around the **same domain** keeps the whole submission cohesive: the user story, the
running application, and the GenAI write-up all reinforce one another.
