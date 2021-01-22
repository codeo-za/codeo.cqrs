const push = requireModule("git-push");

module.exports = async function() {
  try {
    await push({
      dryRun: false, 
      quiet: true
    });
    return true;
  } catch (e) {
    return false;
  }
}