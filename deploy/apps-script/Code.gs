/**
 * Kitaphane email sender — Google Apps Script web app (docs/deployment/deployment.md §3).
 *
 * The backend POSTs {secret, to, fromName, subject, html, text}; the email is sent from the Gmail
 * account this script belongs to, with the sender name "Kitaphane". The secret is stored in
 * Project Settings → Script Properties as SECRET (the same value as APPS_SCRIPT_EMAIL_SECRET on Render).
 *
 * Apps Script web apps always answer 200, so the result is reported as {ok: true} or
 * {ok: false, error: "..."}.
 */

const DEFAULT_SENDER_NAME = 'Kitaphane'
const EMAIL_PATTERN = /^[^\s@,;<>]+@[^\s@,;<>]+\.[^\s@,;<>]+$/

function doPost(e) {
  let request
  try {
    request = JSON.parse(e.postData.contents)
  } catch (error) {
    return reply({ ok: false, error: 'BAD_REQUEST' })
  }

  const secret = PropertiesService.getScriptProperties().getProperty('SECRET')
  if (!secret || typeof request.secret !== 'string' || !constantTimeEquals(request.secret, secret)) {
    return reply({ ok: false, error: 'UNAUTHORIZED' })
  }

  // Exactly one recipient: the script must not become a relay for many addresses.
  if (typeof request.to !== 'string' || !EMAIL_PATTERN.test(request.to)) {
    return reply({ ok: false, error: 'INVALID_RECIPIENT' })
  }
  if (typeof request.subject !== 'string' || typeof request.text !== 'string' || typeof request.html !== 'string') {
    return reply({ ok: false, error: 'BAD_REQUEST' })
  }

  if (MailApp.getRemainingDailyQuota() < 1) {
    return reply({ ok: false, error: 'DAILY_QUOTA_EXCEEDED' })
  }

  try {
    MailApp.sendEmail({
      to: request.to,
      subject: request.subject,
      body: request.text,
      htmlBody: request.html,
      name: typeof request.fromName === 'string' && request.fromName ? request.fromName : DEFAULT_SENDER_NAME,
    })
  } catch (error) {
    return reply({ ok: false, error: 'SEND_FAILED: ' + error.message })
  }

  return reply({ ok: true })
}

/** Run once from the editor to grant the "send email as you" permission (sends nothing). */
function authorize() {
  Logger.log('Remaining daily email quota: ' + MailApp.getRemainingDailyQuota())
}

function constantTimeEquals(a, b) {
  if (a.length !== b.length) return false
  let difference = 0
  for (let i = 0; i < a.length; i++) difference |= a.charCodeAt(i) ^ b.charCodeAt(i)
  return difference === 0
}

function reply(result) {
  return ContentService.createTextOutput(JSON.stringify(result)).setMimeType(ContentService.MimeType.JSON)
}
