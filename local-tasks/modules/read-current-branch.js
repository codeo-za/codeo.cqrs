const git = require("simple-git");

module.exports = function readCurrentBranch() {
  return new Promise((resolve, reject) => {
    git().branch((err, data) => {
      return err
        ? reject(err)
        : resolve(data.current);
    });
  });
}