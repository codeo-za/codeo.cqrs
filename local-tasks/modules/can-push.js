const push = requireModule("git-push");

module.exports = async function() {
  try {
    await push(false, true);
    console.log("can push");
    return true;
  } catch (e) {
    console.log("no pushy");
    return false;
  }
}