FROM ubuntu:22.04

# Create vscode user
ARG USERNAME=vscode

RUN useradd -m $USERNAME

# Set the working directory
RUN mkdir /workspace
WORKDIR /workspace

# Install Python, pip, git, and zsh
RUN apt-get update && apt-get install -y python3 python3-pip git zsh curl
RUN pip install --upgrade pip setuptools wheel

# Copy the entrypoint script
# COPY ../.devcontainer/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh

# Change permissions of the entrypoint script (as root)
# RUN chmod +x /usr/local/bin/docker-entrypoint.sh

# Change ownership of the /app directory to the USERNAME user
# RUN chown -R $USERNAME:$USERNAME /workspace/e2e_samples/fabric_dataops_sample

# Copy configuration files
# COPY ../.devcontainer/config/zshrc /home/$USERNAME/.zshrc
# COPY ../.devcontainer/config/gitconfig /home/$USERNAME/.gitconfig

# Ensure correct permissions for the copied configuration files
# RUN chown $USERNAME:$USERNAME /home/$USERNAME/.zshrc /home/$USERNAME/.gitconfig

# Set Zsh as the default shell for the vscode user
RUN chsh -s $(which zsh) $USERNAME

# Set the user to USERNAME
USER $USERNAME

# Install Oh My Zsh
RUN sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"

# Install zsh plugins auto suggestions and syntax highlighting
RUN git clone https://github.com/zsh-users/zsh-autosuggestions ${ZSH_CUSTOM:-/home/$USERNAME/.oh-my-zsh/custom}/plugins/zsh-autosuggestions

# Set the entrypoint to the script (which will run as the root user)
# ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
