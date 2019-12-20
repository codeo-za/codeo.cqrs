const push = requireModule("git-push");

module.exports = async function() {
  try {
    await push();
    return true;
  } catch (e) {
    return false;
  }
}